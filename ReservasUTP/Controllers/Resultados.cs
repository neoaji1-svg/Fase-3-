using System;
using System.Collections.Generic;
using ReservasUTP.Models;

namespace ReservasUTP.Controllers
{
    /// <summary>Resultado base de los controladores (equivale al dict {"ok", "codigo_error", "error"}).</summary>
    public class Resultado
    {
        public bool Ok { get; set; }
        public string CodigoError { get; set; }
        public string Error { get; set; }

        public static T Falla<T>(string codigo, string mensaje) where T : Resultado, new()
        {
            return new T { Ok = false, CodigoError = codigo, Error = mensaje };
        }
    }

    public class ResultadoLogin : Resultado
    {
        public Usuario Usuario { get; set; }
    }

    public class CategoriaCatalogo
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Restriccion { get; set; }
        public List<Espacio> Espacios { get; set; }
    }

    public class ResultadoCatalogo : Resultado
    {
        public List<CategoriaCatalogo> Categorias { get; set; }
        public int TotalEspacios { get; set; }
    }

    public class CeldaDisponibilidad
    {
        public DateTime Fecha { get; set; }
        public int IdEspacio { get; set; }
        public bool Ocupado { get; set; }
        public string Estado { get { return Ocupado ? "Ocupado" : "Disponible"; } }
    }

    public class FilaDisponibilidad
    {
        public string Bloque { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public List<CeldaDisponibilidad> Celdas { get; set; }
    }

    public class ResultadoDisponibilidad : Resultado
    {
        public string Vista { get; set; }                 // "diaria" | "semanal"
        public Espacio Espacio { get; set; }
        public List<Espacio> EspaciosCategoria { get; set; }
        public List<Espacio> TodosEspacios { get; set; }
        public DateTime Fecha { get; set; }
        public List<string> Columnas { get; set; }        // nombres de espacios (diaria) o días (semanal)
        public List<FilaDisponibilidad> Matriz { get; set; }
    }

    public class ResultadoReserva : Resultado
    {
        public int IdReserva { get; set; }
        public Usuario Usuario { get; set; }
        public Espacio Espacio { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public bool NotificacionEnviada { get; set; }
        public string AvisoCorreo { get; set; }
        public double TiempoMs { get; set; }
    }
}
