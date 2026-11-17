using System.Collections.Generic;
using System.Linq;
using ReservasUTP.Config;
using ReservasUTP.Models;

namespace ReservasUTP.Controllers
{
    /// <summary>
    /// CU-02 Consultar espacios disponibles (RF-02).
    /// Secuencia Fase 2: mostrarEspacios() -> Espacio.obtenerHabilitados().
    /// </summary>
    public static class CtrlConsultarEspacio
    {
        private static readonly string[,] Metadatos =
        {
            { AppConfig.CatTesis,      "Cubículos silenciosos exclusivos para estudiantes en desarrollo de tesis.", "Solo Estudiante Tesista / Administrador" },
            { AppConfig.CatBiblioteca, "Salones de estudio grupal y salón de cómputo en biblioteca.",               "Acceso general comunitario" },
            { AppConfig.CatAulas,      "Salones y laboratorios para tutorías, reposiciones o actividades académicas.", "Acceso general comunitario" },
            { AppConfig.CatGimnasio,   "Cancha multiuso techada y área de acondicionamiento físico.",               "Acceso general comunitario" },
            { AppConfig.CatMusica,     "Salas acústicas equipadas para práctica y ensayo musical.",                 "Acceso general comunitario" },
            { AppConfig.CatAuditorios, "Auditorios institucionales para conferencias, defensas y eventos.",         "Solo Docente / Administrativo / Administrador" }
        };

        public static ResultadoCatalogo MostrarEspacios()
        {
            try
            {
                List<Espacio> habilitados = Espacio.ObtenerHabilitados();
                var porCategoria = habilitados.GroupBy(e => e.Categoria).ToDictionary(g => g.Key, g => g.ToList());
                var categorias = new List<CategoriaCatalogo>();

                for (int i = 0; i < Metadatos.GetLength(0); i++)
                {
                    List<Espacio> lista;
                    porCategoria.TryGetValue(Metadatos[i, 0], out lista);
                    categorias.Add(new CategoriaCatalogo
                    {
                        Nombre = Metadatos[i, 0],
                        Descripcion = Metadatos[i, 1],
                        Restriccion = Metadatos[i, 2],
                        Espacios = lista ?? new List<Espacio>()
                    });
                }

                // Categorías registradas dinámicamente que no estén en los metadatos
                foreach (var par in porCategoria.Where(p => categorias.All(c => c.Nombre != p.Key)))
                {
                    categorias.Add(new CategoriaCatalogo
                    {
                        Nombre = par.Key,
                        Descripcion = "Instalación universitaria registrada.",
                        Restriccion = "Según política institucional",
                        Espacios = par.Value
                    });
                }

                return new ResultadoCatalogo { Ok = true, Categorias = categorias, TotalEspacios = habilitados.Count };
            }
            catch (System.Exception ex)
            {
                return Resultado.Falla<ResultadoCatalogo>("ERROR_BD",
                    "No fue posible cargar el catálogo de espacios: " + ex.Message);
            }
        }
    }
}
