using System;
using System.Windows.Forms;
using ReservasUTP.Config;
using ReservasUTP.Models;
using ReservasUTP.Views;

namespace ReservasUTP
{
    internal static class Program
    {
        /// <summary>Punto de entrada. Argumento opcional "--reset" recrea la BD con los datos de demostración.</summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                bool reset = Array.Exists(args, a => a.Equals("--reset", StringComparison.OrdinalIgnoreCase));
                Db.Inicializar(reset);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo inicializar la base de datos SQL Server.\n\n" + ex.Message +
                    "\n\nVerifique la cadena de conexión \"ReservasUTP\" en ReservasUTP.exe.config.\n" +
                    "Actual: " + AppConfig.ConnectionString,
                    "Reservas UTP - Error de base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ciclo login -> catálogo; al cerrar sesión se vuelve al login.
            while (true)
            {
                using (var login = new FrmLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }
                var catalogo = new FrmCatalogo();
                Application.Run(catalogo);
                if (!catalogo.CerroSesion) return;
            }
        }
    }
}
