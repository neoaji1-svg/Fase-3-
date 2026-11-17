using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ReservasUTP.Models
{
    /// <summary>Entidad Reserva (Fase 2). Trazado a RF-03 / RF-04 / RF-07.</summary>
    public class Reserva
    {
        public int IdReserva { get; set; }
        public int IdUsuario { get; set; }
        public int IdEspacio { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; }

        // Campos de apoyo (JOIN)
        public string UsuarioNombre { get; set; }
        public string EspacioNombre { get; set; }
        public string Categoria { get; set; }

        public double DuracionHoras
        {
            get { return (HoraFin - HoraInicio).TotalHours; }
        }

        private const string SelectBase =
            "SELECT r.id_reserva, r.id_usuario, r.id_espacio, r.fecha, r.hora_inicio, r.hora_fin, " +
            "r.fecha_registro, r.estado, u.nombre, e.nombre, e.categoria " +
            "FROM dbo.Reserva r " +
            "JOIN dbo.Usuario u ON r.id_usuario = u.id_usuario " +
            "JOIN dbo.Espacio e ON r.id_espacio = e.id_espacio ";

        /// <summary>Método trazado en Diagrama de Secuencia CU-03 (Fase 2): reservas activas de un día.</summary>
        public static List<Reserva> ObtenerReservas(int idEspacio, DateTime fecha)
        {
            return ObtenerReservasRango(idEspacio, fecha, fecha);
        }

        /// <summary>Reservas activas de un espacio en un rango de fechas (CU-03 FA-01 vista semanal).</summary>
        public static List<Reserva> ObtenerReservasRango(int idEspacio, DateTime desde, DateTime hasta)
        {
            using (var conn = Db.ObtenerConexion())
            using (var cmd = new SqlCommand(SelectBase +
                "WHERE r.id_espacio = @e AND r.fecha BETWEEN @d AND @h AND r.estado = N'Activa' " +
                "ORDER BY r.fecha, r.hora_inicio", conn))
            {
                cmd.Parameters.Add("@e", SqlDbType.Int).Value = idEspacio;
                cmd.Parameters.Add("@d", SqlDbType.Date).Value = desde.Date;
                cmd.Parameters.Add("@h", SqlDbType.Date).Value = hasta.Date;
                return Leer(cmd);
            }
        }

        /// <summary>
        /// Método trazado en Diagrama de Secuencia CU-04 (Fase 2).
        /// Devuelve true si existe solapamiento. UPDLOCK + HOLDLOCK bloquea el rango
        /// dentro de la transacción (CRT-02, evita doble reserva concurrente).
        /// </summary>
        public static bool ValidarSolapamiento(int idEspacio, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin,
                                               SqlConnection conn, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Reserva WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE id_espacio = @e AND fecha = @f AND estado = N'Activa' " +
                "AND hora_inicio < @hf AND hora_fin > @hi", conn, tx))
            {
                cmd.Parameters.Add("@e", SqlDbType.Int).Value = idEspacio;
                cmd.Parameters.Add("@f", SqlDbType.Date).Value = fecha.Date;
                cmd.Parameters.Add("@hi", SqlDbType.Time).Value = horaInicio;
                cmd.Parameters.Add("@hf", SqlDbType.Time).Value = horaFin;
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        /// <summary>RF-07: horas activas del usuario en la categoría durante la semana (lunes-domingo) de la fecha.</summary>
        public static double CalcularHorasSemanales(int idUsuario, string categoria, DateTime fecha,
                                                    SqlConnection conn, SqlTransaction tx)
        {
            DateTime inicioSemana = InicioSemana(fecha);
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(SUM(DATEDIFF(MINUTE, r.hora_inicio, r.hora_fin)), 0) " +
                "FROM dbo.Reserva r WITH (UPDLOCK, HOLDLOCK) JOIN dbo.Espacio e ON r.id_espacio = e.id_espacio " +
                "WHERE r.id_usuario = @u AND e.categoria = @c AND r.fecha BETWEEN @d AND @h AND r.estado = N'Activa'",
                conn, tx))
            {
                cmd.Parameters.Add("@u", SqlDbType.Int).Value = idUsuario;
                cmd.Parameters.Add("@c", SqlDbType.NVarChar, 50).Value = categoria;
                cmd.Parameters.Add("@d", SqlDbType.Date).Value = inicioSemana;
                cmd.Parameters.Add("@h", SqlDbType.Date).Value = inicioSemana.AddDays(6);
                return Convert.ToInt32(cmd.ExecuteScalar()) / 60.0;
            }
        }

        /// <summary>Método trazado en Diagrama de Secuencia CU-04 (Fase 2): registra la reserva 'Activa'.</summary>
        public static int CrearReserva(int idUsuario, int idEspacio, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin,
                                       SqlConnection conn, SqlTransaction tx)
        {
            using (var cmd = new SqlCommand(
                "INSERT INTO dbo.Reserva (id_usuario, id_espacio, fecha, hora_inicio, hora_fin, estado) " +
                "OUTPUT INSERTED.id_reserva VALUES (@u, @e, @f, @hi, @hf, N'Activa')", conn, tx))
            {
                cmd.Parameters.Add("@u", SqlDbType.Int).Value = idUsuario;
                cmd.Parameters.Add("@e", SqlDbType.Int).Value = idEspacio;
                cmd.Parameters.Add("@f", SqlDbType.Date).Value = fecha.Date;
                cmd.Parameters.Add("@hi", SqlDbType.Time).Value = horaInicio;
                cmd.Parameters.Add("@hf", SqlDbType.Time).Value = horaFin;
                return (int)cmd.ExecuteScalar();
            }
        }

        /// <summary>Historial de reservas del usuario (pantalla "Mis reservas").</summary>
        public static List<Reserva> ObtenerMisReservas(int idUsuario)
        {
            using (var conn = Db.ObtenerConexion())
            using (var cmd = new SqlCommand(SelectBase +
                "WHERE r.id_usuario = @u ORDER BY r.fecha DESC, r.hora_inicio DESC", conn))
            {
                cmd.Parameters.Add("@u", SqlDbType.Int).Value = idUsuario;
                return Leer(cmd);
            }
        }

        public static DateTime InicioSemana(DateTime fecha)
        {
            int delta = ((int)fecha.DayOfWeek + 6) % 7; // Lunes = 0
            return fecha.Date.AddDays(-delta);
        }

        private static List<Reserva> Leer(SqlCommand cmd)
        {
            var lista = new List<Reserva>();
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    lista.Add(new Reserva
                    {
                        IdReserva = rd.GetInt32(0),
                        IdUsuario = rd.GetInt32(1),
                        IdEspacio = rd.GetInt32(2),
                        Fecha = rd.GetDateTime(3),
                        HoraInicio = rd.GetTimeSpan(4),
                        HoraFin = rd.GetTimeSpan(5),
                        FechaRegistro = rd.GetDateTime(6),
                        Estado = rd.GetString(7),
                        UsuarioNombre = rd.GetString(8),
                        EspacioNombre = rd.GetString(9),
                        Categoria = rd.GetString(10)
                    });
                }
            }
            return lista;
        }
    }
}
