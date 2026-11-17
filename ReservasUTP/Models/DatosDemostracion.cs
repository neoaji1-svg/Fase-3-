using System;
using System.Data;
using System.Data.SqlClient;
using ReservasUTP.Config;

namespace ReservasUTP.Models
{
    /// <summary>Datos semilla: usuarios (claves hasheadas), espacios y reservas del 19-oct-2026.</summary>
    internal static class DatosDemostracion
    {
        public static void Cargar(SqlConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                object[,] usuarios =
                {
                    { "1-1-1",       "Carlos Mendoza (Demo)", "carlos.mendoza@utp.ac.pa",   "utp2026",     AppConfig.RolRegular },
                    { "8-1028-886",  "Justin Okada",          "justin.okada@utp.ac.pa",     "tesista2026", AppConfig.RolTesista },
                    { "8-1027-2456", "Gabriel Jimenez",       "gabriel.jimenez@utp.ac.pa",  "clave2026",   AppConfig.RolRegular },
                    { "8-1027-1450", "Maria Townshend",       "maria.townshend@utp.ac.pa",  "tesista2026", AppConfig.RolTesista },
                    { "8-969-172",   "Neo Aji",               "neo.aji@utp.ac.pa",          "clave2026",   AppConfig.RolRegular },
                    { "8-500-101",   "Ing. Elba Valderrama",  "elba.valderrama@utp.ac.pa",  "docente2026", AppConfig.RolDocente },
                    { "8-400-202",   "Lic. Roberto Salas",    "roberto.salas@utp.ac.pa",    "admin2026",   AppConfig.RolAdministrador }
                };
                for (int i = 0; i < usuarios.GetLength(0); i++)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.Usuario (cedula, nombre, correo, password_hash, rol) VALUES (@c, @n, @m, @h, @r)", conn, tx))
                    {
                        cmd.Parameters.Add("@c", SqlDbType.NVarChar, 20).Value = usuarios[i, 0];
                        cmd.Parameters.Add("@n", SqlDbType.NVarChar, 100).Value = usuarios[i, 1];
                        cmd.Parameters.Add("@m", SqlDbType.NVarChar, 100).Value = usuarios[i, 2];
                        cmd.Parameters.Add("@h", SqlDbType.NVarChar, 255).Value = Seguridad.HashPassword((string)usuarios[i, 3]);
                        cmd.Parameters.Add("@r", SqlDbType.NVarChar, 30).Value = usuarios[i, 4];
                        cmd.ExecuteNonQuery();
                    }
                }

                object[,] espacios =
                {
                    { "Salón - 1 (Estudio)", AppConfig.CatBiblioteca, 6, "Pizarra acrílica, Wi-Fi institucional, toma corriente", "Habilitado" },
                    { "Salón - 2 (Estudio)", AppConfig.CatBiblioteca, 6, "Pizarra acrílica, pantalla LED 43 pulgadas", "Habilitado" },
                    { "Salón - 3 (Estudio)", AppConfig.CatBiblioteca, 8, "Mesa grupal, proyector, aire acondicionado", "Habilitado" },
                    { "Salón - 4 (Estudio)", AppConfig.CatBiblioteca, 8, "Mesa grupal, pantalla inteligente, pizarra", "Habilitado" },
                    { "Salón de Cómputo Biblioteca", AppConfig.CatBiblioteca, 20, "20 PCs de escritorio, software académico, Wi-Fi", "Habilitado" },
                    { "Cubículo de Tesis A-01", AppConfig.CatTesis, 2, "Escritorio doble, monitor externo, lámpara, silencio", "Habilitado" },
                    { "Cubículo de Tesis A-02", AppConfig.CatTesis, 2, "Escritorio doble, pizarra de vidrio, Wi-Fi", "Habilitado" },
                    { "Aula 3-104 (Edificio 3)", AppConfig.CatAulas, 35, "Proyector multimedia, pizarra, sistema de audio", "Habilitado" },
                    { "Laboratorio Sistemas 3-201", AppConfig.CatAulas, 30, "30 estaciones de trabajo, proyector, red LAN", "Habilitado" },
                    { "Cancha Multiuso Techada", AppConfig.CatGimnasio, 40, "Tableros baloncesto, red voleibol, marcador", "Habilitado" },
                    { "Área de Pesas y Cardio", AppConfig.CatGimnasio, 25, "Máquinas cardiovasculares, mancuernas, colchonetas", "Habilitado" },
                    { "Salón de Ensayo Musical 1", AppConfig.CatMusica, 15, "Acústica tratada, piano vertical, atriles, consola", "Habilitado" },
                    { "Auditorio Principal FISC", AppConfig.CatAuditorios, 250, "Sistema de sonido envolvente, proyector láser, podio", "Habilitado" },
                    // Espacio inhabilitado para demostrar EX-01 en CU-03 y CU-04
                    { "Salón - 5 (Mantenimiento)", AppConfig.CatBiblioteca, 6, "En remodelación eléctrica temporal", "Inhabilitado" }
                };
                for (int i = 0; i < espacios.GetLength(0); i++)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.Espacio (nombre, categoria, capacidad, equipamiento, estado) VALUES (@n, @c, @cap, @eq, @e)", conn, tx))
                    {
                        cmd.Parameters.Add("@n", SqlDbType.NVarChar, 100).Value = espacios[i, 0];
                        cmd.Parameters.Add("@c", SqlDbType.NVarChar, 50).Value = espacios[i, 1];
                        cmd.Parameters.Add("@cap", SqlDbType.Int).Value = espacios[i, 2];
                        cmd.Parameters.Add("@eq", SqlDbType.NVarChar, 500).Value = espacios[i, 3];
                        cmd.Parameters.Add("@e", SqlDbType.NVarChar, 20).Value = espacios[i, 4];
                        cmd.ExecuteNonQuery();
                    }
                }

                // Reservas del 19-oct-2026 (cuadrícula del prototipo, Fase 1 pág. 27)
                int[,] reservas =
                {
                    // id_usuario, id_espacio, hora_inicio, hora_fin
                    { 3, 4,  8,  9 }, { 3, 1, 10, 11 }, { 5, 3, 10, 11 }, { 3, 1, 11, 12 },
                    { 5, 1, 12, 13 }, { 3, 2, 13, 14 }, { 5, 3, 13, 14 }
                };
                for (int i = 0; i < reservas.GetLength(0); i++)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO dbo.Reserva (id_usuario, id_espacio, fecha, hora_inicio, hora_fin, estado) " +
                        "VALUES (@u, @e, @f, @hi, @hf, N'Activa')", conn, tx))
                    {
                        cmd.Parameters.Add("@u", SqlDbType.Int).Value = reservas[i, 0];
                        cmd.Parameters.Add("@e", SqlDbType.Int).Value = reservas[i, 1];
                        cmd.Parameters.Add("@f", SqlDbType.Date).Value = new DateTime(2026, 10, 19);
                        cmd.Parameters.Add("@hi", SqlDbType.Time).Value = new TimeSpan(reservas[i, 2], 0, 0);
                        cmd.Parameters.Add("@hf", SqlDbType.Time).Value = new TimeSpan(reservas[i, 3], 0, 0);
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
        }
    }
}
