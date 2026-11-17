using System;
using System.IO;
using System.Text;
using ReservasUTP.Config;

namespace ReservasUTP.Models
{
    /// <summary>EX-04 de CU-04: fallo no bloqueante del servicio de correo.</summary>
    public class ErrorServicioCorreo : Exception
    {
        public ErrorServicioCorreo(string mensaje) : base(mensaje) { }
    }

    /// <summary>
    /// Componente "Servicio de Correo" de la Matriz de Trazabilidad (RF-08).
    /// En el prototipo se simula el SMTP escribiendo en logs/notificaciones_correo.log.
    /// </summary>
    public static class ServicioCorreo
    {
        public static void EnviarConfirmacion(Usuario usuario, Espacio espacio, DateTime fecha,
                                              TimeSpan horaInicio, TimeSpan horaFin, int idReserva,
                                              bool simularFallo = false)
        {
            if (simularFallo)
                throw new ErrorServicioCorreo(
                    "EX-04: El servidor SMTP institucional no respondió temporalmente. La reserva se conservó.");

            Directory.CreateDirectory(Path.GetDirectoryName(AppConfig.MailLogPath));
            string linea = string.Format(
                "[{0:yyyy-MM-dd HH:mm:ss}] PARA: {1} | ASUNTO: Confirmación Reserva #{2} - UTP | Usuario: {3} | " +
                "Espacio: {4} ({5}) | Fecha: {6:yyyy-MM-dd} | Horario: {7:hh\\:mm} a {8:hh\\:mm}{9}",
                DateTime.Now, usuario.Correo, idReserva, usuario.Nombre, espacio.Nombre, espacio.Categoria,
                fecha, horaInicio, horaFin, Environment.NewLine);
            File.AppendAllText(AppConfig.MailLogPath, linea, Encoding.UTF8);
        }
    }
}
