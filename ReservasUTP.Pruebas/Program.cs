using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ReservasUTP.Config;
using ReservasUTP.Controllers;
using ReservasUTP.Models;

namespace ReservasUTP.Pruebas
{
    /// <summary>
    /// Suite de pruebas funcionales de los CU (equivalente a tests/test_casos_uso.py).
    /// Usa una BD aislada (cadena "ReservasUTP_Pruebas" en App.config) que se recrea en cada ejecución.
    /// </summary>
    internal static class Program
    {
        private static int _ok, _fallas;
        private static readonly DateTime Lunes19 = new DateTime(2026, 10, 19);

        private static int Main()
        {
            var cs = System.Configuration.ConfigurationManager.ConnectionStrings["ReservasUTP_Pruebas"];
            if (cs != null) AppConfig.ConnectionString = cs.ConnectionString;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("BD de pruebas: " + new SqlConnectionStringBuilder(AppConfig.ConnectionString).InitialCatalog);
            Db.Inicializar(forceReset: true);

            var pruebas = new List<KeyValuePair<string, Action>>
            {
                P("test_01 RNF-03 y CU-01: hash PBKDF2 y autenticación", Test01),
                P("test_02 CU-02: catálogo de espacios habilitados", Test02),
                P("test_03 CU-03: vista diaria, semanal (FA-01), EX-01 y EX-02", Test03),
                P("test_04 CU-04 flujo principal y RNF-01 (< 3 s)", Test04),
                P("test_05 CU-04 EX-01 solapamiento de horario", Test05),
                P("test_06 CU-04 RF-05 restricción por rol (FA-01 / EX-02)", Test06),
                P("test_07 CU-04 RF-07 límite semanal (FA-02 / EX-03)", Test07),
                P("test_08 CU-04 EX-04 fallo de correo no bloqueante", Test08)
            };

            foreach (var p in pruebas)
            {
                try
                {
                    p.Value();
                    _ok++;
                    Console.WriteLine("[PASS] " + p.Key);
                }
                catch (Exception ex)
                {
                    _fallas++;
                    Console.WriteLine("[FAIL] " + p.Key + " -> " + ex.Message);
                }
            }
            Console.WriteLine();
            Console.WriteLine("Resultado: " + _ok + " OK, " + _fallas + " fallidas, " + pruebas.Count + " total.");
            return _fallas == 0 ? 0 : 1;
        }

        private static KeyValuePair<string, Action> P(string n, Action a) { return new KeyValuePair<string, Action>(n, a); }

        private static void Assert(bool cond, string msg) { if (!cond) throw new Exception(msg); }

        private static void Test01()
        {
            using (var conn = Db.ObtenerConexion())
            using (var cmd = new SqlCommand("SELECT password_hash FROM dbo.Usuario", conn))
            using (var rd = cmd.ExecuteReader())
            {
                int n = 0;
                while (rd.Read())
                {
                    n++;
                    string h = rd.GetString(0);
                    Assert(h.StartsWith("pbkdf2:sha256:"), "Hash sin formato PBKDF2");
                    Assert(!h.Contains("utp2026") && !h.Contains("tesista2026"), "Contraseña en texto plano");
                }
                Assert(n >= 5, "Faltan usuarios semilla");
            }
            Assert(CtrlAuth.IniciarSesion("1-1-1", "utp2026").Ok, "Login válido rechazado");
            Assert(!CtrlAuth.IniciarSesion("1-1-1", "incorrecta").Ok, "Login inválido aceptado");
            Assert(!CtrlAuth.IniciarSesion("", "").Ok, "Campos vacíos aceptados");
        }

        private static void Test02()
        {
            var r = CtrlConsultarEspacio.MostrarEspacios();
            Assert(r.Ok, r.Error);
            var nombres = r.Categorias.SelectMany(c => c.Espacios).Select(e => e.Nombre).ToList();
            Assert(r.Categorias.Any(c => c.Nombre == AppConfig.CatTesis), "Falta categoría Cubículos de Tesis");
            Assert(nombres.Contains("Salón - 4 (Estudio)"), "Falta Salón - 4");
            Assert(!nombres.Contains("Salón - 5 (Mantenimiento)"), "Se mostró un espacio inhabilitado");
            Assert(r.TotalEspacios == 13, "Se esperaban 13 espacios habilitados");
        }

        private static void Test03()
        {
            var diaria = CtrlConsultarHorario.ConsultarDisponibilidad(4, Lunes19, CtrlConsultarHorario.VistaDiaria);
            Assert(diaria.Ok, diaria.Error);
            int col = diaria.EspaciosCategoria.FindIndex(e => e.IdEspacio == 4);
            Assert(diaria.Matriz[0].Celdas[col].Ocupado, "Salón-4 08:00 debería estar Ocupado");
            Assert(!diaria.Matriz[4].Celdas[col].Ocupado, "Salón-4 12:00 debería estar Disponible");

            var semanal = CtrlConsultarHorario.ConsultarDisponibilidad(4, Lunes19, CtrlConsultarHorario.VistaSemanal);
            Assert(semanal.Ok && semanal.Columnas.Count == 7, "Vista semanal inválida");
            Assert(semanal.Columnas[6].EndsWith("2026-10-25"), "La semana debe terminar el 2026-10-25");

            var ex01 = CtrlConsultarHorario.ConsultarDisponibilidad(14, Lunes19);
            Assert(!ex01.Ok && ex01.CodigoError == "EX-01", "No se produjo EX-01");

            var ex02 = CtrlConsultarHorario.ConsultarDisponibilidad(4, Lunes19, CtrlConsultarHorario.VistaDiaria, true);
            Assert(!ex02.Ok && ex02.CodigoError == "EX-02", "No se produjo EX-02");
        }

        private static void Test04()
        {
            var r = CtrlCrearReserva.ProcesarReserva(1, 4, Lunes19, H(12), H(13));
            Assert(r.Ok, r.Error);
            Assert(r.Espacio.Nombre == "Salón - 4 (Estudio)", "Espacio incorrecto");
            Assert(r.TiempoMs < 3000, "RNF-01 excedido: " + r.TiempoMs + " ms");
            Console.WriteLine("       Reserva #" + r.IdReserva + " registrada en " + r.TiempoMs.ToString("0.00") + " ms");
        }

        private static void Test05()
        {
            var r = CtrlCrearReserva.ProcesarReserva(2, 4, Lunes19, H(12), H(13));
            Assert(!r.Ok && r.CodigoError == "EX-01" && r.Error.Contains("Horario ocupado"), "No se bloqueó el solapamiento");
        }

        private static void Test06()
        {
            var reg = CtrlCrearReserva.ProcesarReserva(1, 6, Lunes19.AddDays(1), H(9), H(10));
            Assert(!reg.Ok && reg.CodigoError == "EX-02", "Estudiante Regular pudo reservar Cubículo de Tesis");
            var tes = CtrlCrearReserva.ProcesarReserva(2, 6, Lunes19.AddDays(1), H(9), H(10));
            Assert(tes.Ok, "Tesista no pudo reservar: " + tes.Error);
        }

        private static void Test07()
        {
            var ok = CtrlCrearReserva.ProcesarReserva(1, 4, Lunes19.AddDays(1), H(8), H(13));
            Assert(ok.Ok, "FA-02 debería permitir llegar a 6 h: " + ok.Error);
            var ex03 = CtrlCrearReserva.ProcesarReserva(1, 4, Lunes19.AddDays(2), H(8), H(9));
            Assert(!ex03.Ok && ex03.CodigoError == "EX-03", "No se produjo EX-03");
        }

        private static void Test08()
        {
            var r = CtrlCrearReserva.ProcesarReserva(6, 13, Lunes19.AddDays(3), H(10), H(12), simularFalloCorreo: true);
            Assert(r.Ok, r.Error);
            Assert(!r.NotificacionEnviada && r.AvisoCorreo.Contains("EX-04"), "EX-04 no reportado");
            Assert(Reserva.ObtenerMisReservas(6).Any(x => x.IdReserva == r.IdReserva && x.Estado == "Activa"),
                "La reserva no se conservó en BD");
        }

        private static TimeSpan H(int h) { return new TimeSpan(h, 0, 0); }
    }
}
