using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using ReservasUTP.Config;
using ReservasUTP.Models;

namespace ReservasUTP.Controllers
{
    /// <summary>
    /// CU-04 Crear reserva (RF-04, RF-05, RF-07, RF-08).
    /// Secuencia Fase 2: procesarReserva(idUsuario, idEspacio, fecha, horaInicio, horaFin).
    /// </summary>
    public static class CtrlCrearReserva
    {
        public static ResultadoReserva ProcesarReserva(int idUsuario, int idEspacio, DateTime fecha,
            TimeSpan horaInicio, TimeSpan horaFin, bool simularFalloCorreo = false)
        {
            var reloj = Stopwatch.StartNew();
            double duracion = (horaFin - horaInicio).TotalHours;

            // Validaciones de entrada
            if (duracion <= 0)
                return Resultado.Falla<ResultadoReserva>("VALIDACION", "La hora de fin debe ser posterior a la hora de inicio.");
            var primero = AppConfig.BloquesHorarios.First();
            var ultimo = AppConfig.BloquesHorarios.Last();
            if (horaInicio < primero.Inicio || horaFin > ultimo.Fin)
                return Resultado.Falla<ResultadoReserva>("VALIDACION",
                    "El horario debe estar dentro de la franja institucional 08:00 - 18:00.");

            try
            {
                Usuario usuario = Usuario.ObtenerPorId(idUsuario);
                if (usuario == null)
                    return Resultado.Falla<ResultadoReserva>("AUTH", "Usuario no autenticado o inexistente.");

                Espacio espacio = Espacio.ObtenerPorId(idEspacio);
                if (espacio == null)
                    return Resultado.Falla<ResultadoReserva>("ESPACIO_NO_EXISTE", "El espacio seleccionado no existe.");

                // Paso 5: el espacio sigue habilitado
                if (!Espacio.SigueHabilitado(idEspacio))
                    return Resultado.Falla<ResultadoReserva>("EX-01",
                        "EX-01: El espacio '" + espacio.Nombre + "' fue inhabilitado y no admite reservas.");

                // Paso 7 (RF-05 / FA-01 / EX-02): restricción por rol
                string rol = Usuario.VerificarRol(idUsuario);
                string[] permitidos = AppConfig.ObtenerRolesPermitidos(espacio.Categoria);
                if (!permitidos.Contains(rol))
                    return Resultado.Falla<ResultadoReserva>("EX-02",
                        "EX-02 (Rol no autorizado): Su rol '" + rol + "' no tiene autorización para reservar espacios " +
                        "de la categoría '" + espacio.Categoria + "'. Roles permitidos: " + string.Join(", ", permitidos) + ".");

                int idReserva;
                // Transacción atómica (CRT-02) con aislamiento serializable
                using (SqlConnection conn = Db.ObtenerConexion())
                using (SqlTransaction tx = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    // Paso 6 / EX-01: solapamiento
                    if (Reserva.ValidarSolapamiento(idEspacio, fecha, horaInicio, horaFin, conn, tx))
                    {
                        tx.Rollback();
                        return Resultado.Falla<ResultadoReserva>("EX-01",
                            string.Format("EX-01 (Horario ocupado): El bloque {0:hh\\:mm} - {1:hh\\:mm} del {2:yyyy-MM-dd} en '{3}' ya se encuentra reservado.",
                                horaInicio, horaFin, fecha, espacio.Nombre));
                    }

                    // Paso 8 (RF-07 / FA-02 / EX-03): límite semanal
                    int limite = AppConfig.ObtenerLimiteSemanal(rol, espacio.Categoria);
                    double acumuladas = Reserva.CalcularHorasSemanales(idUsuario, espacio.Categoria, fecha, conn, tx);
                    if (acumuladas + duracion > limite)
                    {
                        tx.Rollback();
                        return Resultado.Falla<ResultadoReserva>("EX-03",
                            string.Format("EX-03 (Límite semanal excedido): Ya tiene {0:0.0} h activas esta semana en '{1}'. " +
                                          "Esta reserva ({2:0.0} h) superaría el límite institucional de {3} h/semana para el rol '{4}'.",
                                acumuladas, espacio.Categoria, duracion, limite, rol));
                    }

                    // Pasos 10-11: registrar reserva 'Activa'
                    idReserva = Reserva.CrearReserva(idUsuario, idEspacio, fecha, horaInicio, horaFin, conn, tx);
                    tx.Commit();
                }

                // Paso 13 (RF-08 / EX-04): notificación por correo (no bloqueante)
                bool enviada = true;
                string aviso = "Información enviada a su correo institucional (" + usuario.Correo + ").";
                try
                {
                    ServicioCorreo.EnviarConfirmacion(usuario, espacio, fecha, horaInicio, horaFin, idReserva, simularFalloCorreo);
                }
                catch (ErrorServicioCorreo ex)
                {
                    enviada = false;
                    aviso = ex.Message;
                }

                reloj.Stop();
                return new ResultadoReserva
                {
                    Ok = true,
                    IdReserva = idReserva,
                    Usuario = usuario,
                    Espacio = espacio,
                    Fecha = fecha.Date,
                    HoraInicio = horaInicio,
                    HoraFin = horaFin,
                    NotificacionEnviada = enviada,
                    AvisoCorreo = aviso,
                    TiempoMs = reloj.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                return Resultado.Falla<ResultadoReserva>("ERROR_BD", "Error interno al registrar la reserva: " + ex.Message);
            }
        }
    }
}
