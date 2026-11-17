using System;
using System.Collections.Generic;
using System.Linq;
using ReservasUTP.Config;
using ReservasUTP.Models;

namespace ReservasUTP.Controllers
{
    /// <summary>
    /// CU-03 Consultar disponibilidad de un espacio (RF-03).
    /// Secuencia Fase 2: consultarDisponibilidad(idEspacio, fecha) -> Espacio.obtenerEstado() -> Reserva.obtenerReservas().
    /// </summary>
    public static class CtrlConsultarHorario
    {
        public const string VistaDiaria = "diaria";
        public const string VistaSemanal = "semanal";

        public static ResultadoDisponibilidad ConsultarDisponibilidad(int idEspacio, DateTime fecha,
            string vista = VistaDiaria, bool simularErrorBd = false)
        {
            try
            {
                if (simularErrorBd)
                    throw new InvalidOperationException("Fallo simulado de conexión al consultar disponibilidad.");

                Espacio espacio = Espacio.ObtenerPorId(idEspacio);
                if (espacio == null)
                    return Resultado.Falla<ResultadoDisponibilidad>("EX-01",
                        "EX-01: El espacio solicitado no existe en el catálogo institucional.");

                // EX-01: espacio inhabilitado
                if (Espacio.ObtenerEstado(idEspacio) != "Habilitado")
                {
                    var r = Resultado.Falla<ResultadoDisponibilidad>("EX-01",
                        "EX-01: El espacio '" + espacio.Nombre + "' se encuentra actualmente Inhabilitado y no está disponible para consulta ni reserva.");
                    r.Espacio = espacio;
                    return r;
                }

                List<Espacio> todos = Espacio.ObtenerHabilitados();
                List<Espacio> deCategoria = todos.Where(e => e.Categoria == espacio.Categoria).ToList();
                var resultado = new ResultadoDisponibilidad
                {
                    Ok = true,
                    Espacio = espacio,
                    EspaciosCategoria = deCategoria,
                    TodosEspacios = todos,
                    Fecha = fecha.Date,
                    Matriz = new List<FilaDisponibilidad>()
                };

                if (vista == VistaSemanal)
                {
                    // FA-01: vista semanal (lunes a domingo) del espacio seleccionado
                    DateTime lunes = Reserva.InicioSemana(fecha);
                    List<DateTime> dias = Enumerable.Range(0, 7).Select(i => lunes.AddDays(i)).ToList();
                    List<Reserva> reservas = Reserva.ObtenerReservasRango(idEspacio, dias[0], dias[6]);

                    resultado.Vista = VistaSemanal;
                    resultado.Columnas = dias.Select(d => NombreDia(d) + " " + d.ToString("yyyy-MM-dd")).ToList();
                    foreach (var b in AppConfig.BloquesHorarios)
                    {
                        resultado.Matriz.Add(new FilaDisponibilidad
                        {
                            Bloque = b.Etiqueta, HoraInicio = b.Inicio, HoraFin = b.Fin,
                            Celdas = dias.Select(d => new CeldaDisponibilidad
                            {
                                Fecha = d, IdEspacio = idEspacio,
                                Ocupado = reservas.Any(r => r.Fecha.Date == d && Solapa(r, b))
                            }).ToList()
                        });
                    }
                    return resultado;
                }

                // Vista diaria: cuadrícula comparativa de todos los espacios de la categoría (prototipo Fase 1)
                var reservasPorEspacio = deCategoria.ToDictionary(e => e.IdEspacio,
                    e => Reserva.ObtenerReservas(e.IdEspacio, fecha));

                resultado.Vista = VistaDiaria;
                resultado.Columnas = deCategoria.Select(e => e.Nombre).ToList();
                foreach (var b in AppConfig.BloquesHorarios)
                {
                    resultado.Matriz.Add(new FilaDisponibilidad
                    {
                        Bloque = b.Etiqueta, HoraInicio = b.Inicio, HoraFin = b.Fin,
                        Celdas = deCategoria.Select(e => new CeldaDisponibilidad
                        {
                            Fecha = fecha.Date, IdEspacio = e.IdEspacio,
                            Ocupado = reservasPorEspacio[e.IdEspacio].Any(r => Solapa(r, b))
                        }).ToList()
                    });
                }
                return resultado;
            }
            catch (Exception ex)
            {
                // EX-02: error al recuperar disponibilidad
                return Resultado.Falla<ResultadoDisponibilidad>("EX-02",
                    "EX-02: No fue posible consultar la disponibilidad en este momento (" + ex.Message +
                    "). No se modificó ningún dato.");
            }
        }

        /// <summary>Ajuste justificado: una reserva de varias horas marca todos los bloques que cubre.</summary>
        private static bool Solapa(Reserva r, BloqueHorario b)
        {
            return r.HoraInicio < b.Fin && r.HoraFin > b.Inicio;
        }

        private static string NombreDia(DateTime d)
        {
            string[] nombres = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };
            return nombres[(int)d.DayOfWeek];
        }
    }
}
