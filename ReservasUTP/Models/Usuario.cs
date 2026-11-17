using System.Data;
using System.Data.SqlClient;

namespace ReservasUTP.Models
{
    /// <summary>Entidad Usuario (Fase 2, Modelo de Datos). Trazado a RF-01 / RF-05.</summary>
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }

        /// <summary>CU-01: valida cédula + clave contra el hash almacenado.</summary>
        public static Usuario Autenticar(string cedula, string clave)
        {
            using (var conn = Db.ObtenerConexion())
            using (var cmd = new SqlCommand(
                "SELECT id_usuario, cedula, nombre, correo, password_hash, rol FROM dbo.Usuario WHERE cedula = @c", conn))
            {
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 20).Value = (cedula ?? "").Trim();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    if (!Seguridad.VerifyPassword(clave ?? "", rd.GetString(4))) return null;
                    return Mapear(rd);
                }
            }
        }

        public static Usuario ObtenerPorId(int idUsuario)
        {
            using (var conn = Db.ObtenerConexion())
            using (var cmd = new SqlCommand(
                "SELECT id_usuario, cedula, nombre, correo, password_hash, rol FROM dbo.Usuario WHERE id_usuario = @id", conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = idUsuario;
                using (var rd = cmd.ExecuteReader())
                    return rd.Read() ? Mapear(rd) : null;
            }
        }

        /// <summary>Método trazado en Diagrama de Secuencia CU-04 (Fase 2).</summary>
        public static string VerificarRol(int idUsuario)
        {
            var u = ObtenerPorId(idUsuario);
            return u != null ? u.Rol : null;
        }

        private static Usuario Mapear(SqlDataReader rd)
        {
            return new Usuario
            {
                IdUsuario = rd.GetInt32(0),
                Cedula = rd.GetString(1),
                Nombre = rd.GetString(2),
                Correo = rd.GetString(3),
                Rol = rd.GetString(5)
            };
        }
    }
}
