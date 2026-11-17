using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ReservasUTP.Config
{
    /// <summary>Bloque horario institucional (08:00 - 18:00, bloques de 1 h).</summary>
    public class BloqueHorario
    {
        public TimeSpan Inicio { get; private set; }
        public TimeSpan Fin { get; private set; }

        public BloqueHorario(int horaInicio, int horaFin)
        {
            Inicio = new TimeSpan(horaInicio, 0, 0);
            Fin = new TimeSpan(horaFin, 0, 0);
        }

        public string Etiqueta
        {
            get { return Inicio.ToString(@"hh\:mm") + " - " + Fin.ToString(@"hh\:mm"); }
        }
    }

    /// <summary>
    /// Configuración central y reglas de negocio (RF-05 permisos por rol, RF-07 límites semanales).
    /// Equivalente a config.py del prototipo anterior.
    /// </summary>
    public static class AppConfig
    {
        private static string _connectionStringOverride;

        /// <summary>Cadena de conexión a SQL Server (App.config: "ReservasUTP").</summary>
        public static string ConnectionString
        {
            get
            {
                if (!string.IsNullOrEmpty(_connectionStringOverride)) return _connectionStringOverride;
                var cs = ConfigurationManager.ConnectionStrings["ReservasUTP"];
                return cs != null
                    ? cs.ConnectionString
                    : @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ReservasUTP;Integrated Security=True";
            }
            set { _connectionStringOverride = value; }
        }

        public static string BaseDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string SchemaSqlPath
        {
            get { return Path.Combine(BaseDir, "Database", "schema_sqlserver.sql"); }
        }

        public static string MailLogPath
        {
            get { return Path.Combine(BaseDir, "logs", "notificaciones_correo.log"); }
        }

        public static readonly List<BloqueHorario> BloquesHorarios = new List<BloqueHorario>
        {
            new BloqueHorario(8, 9),   new BloqueHorario(9, 10),  new BloqueHorario(10, 11),
            new BloqueHorario(11, 12), new BloqueHorario(12, 13), new BloqueHorario(13, 14),
            new BloqueHorario(14, 15), new BloqueHorario(15, 16), new BloqueHorario(16, 17),
            new BloqueHorario(17, 18)
        };

        public const string RolRegular = "Estudiante Regular";
        public const string RolTesista = "Estudiante Tesista";
        public const string RolDocente = "Docente";
        public const string RolAdministrativo = "Administrativo";
        public const string RolAdministrador = "Administrador";

        public const string CatTesis = "Cubículos de Tesis";
        public const string CatBiblioteca = "Servicios de Biblioteca";
        public const string CatAulas = "Aulas de Clase";
        public const string CatGimnasio = "Gimnasio";
        public const string CatMusica = "Salón de Música";
        public const string CatAuditorios = "Auditorios";

        private static readonly string[] TodosLosRoles =
            { RolRegular, RolTesista, RolDocente, RolAdministrativo, RolAdministrador };

        /// <summary>RF-05: roles autorizados por categoría de espacio.</summary>
        public static readonly Dictionary<string, string[]> PermisosCategoria = new Dictionary<string, string[]>
        {
            { CatTesis,      new[] { RolTesista, RolAdministrador } },
            { CatBiblioteca, TodosLosRoles },
            { CatAulas,      TodosLosRoles },
            { CatGimnasio,   TodosLosRoles },
            { CatMusica,     TodosLosRoles },
            { CatAuditorios, new[] { RolDocente, RolAdministrativo, RolAdministrador } }
        };

        /// <summary>RF-07: límite máximo de horas semanales por (rol|categoría).</summary>
        public static readonly Dictionary<string, int> LimitesSemanalesHoras = new Dictionary<string, int>();

        static AppConfig()
        {
            foreach (var cat in new[] { CatBiblioteca, CatAulas, CatGimnasio, CatMusica })
            {
                Limite(RolRegular, cat, 6);
                Limite(RolTesista, cat, 6);
                Limite(RolDocente, cat, 12);
                Limite(RolAdministrativo, cat, 12);
            }
            Limite(RolTesista, CatTesis, 10);
            Limite(RolDocente, CatAuditorios, 8);
            Limite(RolAdministrativo, CatAuditorios, 8);
            foreach (var cat in PermisosCategoria.Keys) Limite(RolAdministrador, cat, 40);
        }

        private static void Limite(string rol, string categoria, int horas)
        {
            LimitesSemanalesHoras[rol + "|" + categoria] = horas;
        }

        public static int ObtenerLimiteSemanal(string rol, string categoria)
        {
            int horas;
            return LimitesSemanalesHoras.TryGetValue(rol + "|" + categoria, out horas) ? horas : 6;
        }

        public static string[] ObtenerRolesPermitidos(string categoria)
        {
            string[] roles;
            return PermisosCategoria.TryGetValue(categoria, out roles) ? roles : new string[0];
        }
    }
}
