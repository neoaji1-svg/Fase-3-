using System;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ReservasUTP.Config;

namespace ReservasUTP.Models
{
    /// <summary>Acceso a SQL Server (equivalente a models/db.py).</summary>
    public static class Db
    {
        public static SqlConnection ObtenerConexion()
        {
            var conn = new SqlConnection(AppConfig.ConnectionString);
            conn.Open();
            return conn;
        }

        /// <summary>
        /// Crea la base de datos si no existe, ejecuta el DDL y carga datos de demostración
        /// si la tabla Usuario está vacía. forceReset borra y recrea las tablas.
        /// </summary>
        public static void Inicializar(bool forceReset = false)
        {
            var builder = new SqlConnectionStringBuilder(AppConfig.ConnectionString);
            string nombreBd = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(nombreBd))
                throw new InvalidOperationException("La cadena de conexión debe indicar Initial Catalog.");

            // 1. Crear BD si no existe (conectando a master)
            var masterBuilder = new SqlConnectionStringBuilder(AppConfig.ConnectionString) { InitialCatalog = "master" };
            using (var master = new SqlConnection(masterBuilder.ConnectionString))
            {
                master.Open();
                string nombreSeguro = nombreBd.Replace("]", "]]");
                using (var cmd = new SqlCommand(
                    "IF DB_ID(@n) IS NULL EXEC('CREATE DATABASE [" + nombreSeguro.Replace("'", "''") + "]');", master))
                {
                    cmd.Parameters.AddWithValue("@n", nombreBd);
                    cmd.ExecuteNonQuery();
                }
            }

            using (var conn = ObtenerConexion())
            {
                if (forceReset)
                {
                    Ejecutar(conn,
                        "IF OBJECT_ID(N'dbo.Reserva', N'U') IS NOT NULL DROP TABLE dbo.Reserva;" +
                        "IF OBJECT_ID(N'dbo.Espacio', N'U') IS NOT NULL DROP TABLE dbo.Espacio;" +
                        "IF OBJECT_ID(N'dbo.Usuario', N'U') IS NOT NULL DROP TABLE dbo.Usuario;");
                }

                // 2. DDL (separado por GO)
                string script = File.ReadAllText(AppConfig.SchemaSqlPath, Encoding.UTF8);
                foreach (string lote in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(lote)) Ejecutar(conn, lote);
                }

                // 3. Datos semilla
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.Usuario", conn))
                {
                    if ((int)cmd.ExecuteScalar() == 0) DatosDemostracion.Cargar(conn);
                }
            }
        }

        internal static void Ejecutar(SqlConnection conn, string sql, SqlTransaction tx = null)
        {
            using (var cmd = new SqlCommand(sql, conn, tx)) cmd.ExecuteNonQuery();
        }
    }

    /// <summary>RNF-03: contraseñas con PBKDF2-HMAC-SHA256 + salt aleatorio (nunca en texto plano).</summary>
    public static class Seguridad
    {
        private const int Iteraciones = 100000;

        public static string HashPassword(string password)
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(saltBytes);
            string salt = ToHex(saltBytes);
            byte[] dk = Derivar(password, salt, Iteraciones);
            return "pbkdf2:sha256:" + Iteraciones + "$" + salt + "$" + ToHex(dk);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                string[] partes = storedHash.Split('$');
                if (partes.Length != 3) return false;
                int iteraciones = int.Parse(partes[0].Split(':')[2]);
                string calculado = ToHex(Derivar(password, partes[1], iteraciones));
                return ComparacionConstante(calculado, partes[2]);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] Derivar(string password, string salt, int iteraciones)
        {
            using (var kdf = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password),
                       Encoding.UTF8.GetBytes(salt), iteraciones, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(32);
            }
        }

        private static bool ComparacionConstante(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
