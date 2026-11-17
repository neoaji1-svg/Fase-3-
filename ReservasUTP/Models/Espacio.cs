using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ReservasUTP.Models
{
    /// <summary>Entidad Espacio (Fase 2). Trazado a RF-02 / RF-03 / RF-04.</summary>
    public class Espacio
    {
        public int IdEspacio { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public int Capacidad { get; set; }
        public string Equipamiento { get; set; }
        public string Estado { get; set; }

        private const string Columnas = "id_espacio, nombre, categoria, capacidad, equipamiento, estado";

        /// <summary>Método trazado en Diagrama de Secuencia CU-02 (Fase 2).</summary>
        public static List<Espacio> ObtenerHabilitados()
        {
            return Consultar("SELECT " + Columnas + " FROM dbo.Espacio WHERE estado = N'Habilitado' ORDER BY categoria, id_espacio", null);
        }

        public static List<Espacio> ObtenerTodos()
        {
            return Consultar("SELECT " + Columnas + " FROM dbo.Espacio ORDER BY categoria, id_espacio", null);
        }

        public static Espacio ObtenerPorId(int idEspacio)
        {
            var lista = Consultar("SELECT " + Columnas + " FROM dbo.Espacio WHERE id_espacio = @id", idEspacio);
            return lista.Count > 0 ? lista[0] : null;
        }

        /// <summary>Método trazado en Diagrama de Secuencia CU-03 (Fase 2).</summary>
        public static string ObtenerEstado(int idEspacio)
        {
            var e = ObtenerPorId(idEspacio);
            return e != null ? e.Estado : null;
        }

        /// <summary>Método trazado en Diagrama de Secuencia CU-04 (Fase 2).</summary>
        public static bool SigueHabilitado(int idEspacio)
        {
            return ObtenerEstado(idEspacio) == "Habilitado";
        }

        public override string ToString()
        {
            return Nombre;
        }

        private static List<Espacio> Consultar(string sql, int? id)
        {
            var lista = new List<Espacio>();
            using (var conn = Db.ObtenerConexion())
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (id.HasValue) cmd.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        lista.Add(new Espacio
                        {
                            IdEspacio = rd.GetInt32(0),
                            Nombre = rd.GetString(1),
                            Categoria = rd.GetString(2),
                            Capacidad = rd.GetInt32(3),
                            Equipamiento = rd.IsDBNull(4) ? "" : rd.GetString(4),
                            Estado = rd.GetString(5)
                        });
                    }
                }
            }
            return lista;
        }
    }
}
