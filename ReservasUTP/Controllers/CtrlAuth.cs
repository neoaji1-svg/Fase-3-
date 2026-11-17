using ReservasUTP.Models;

namespace ReservasUTP.Controllers
{
    /// <summary>Controlador de Autenticación (RF-01 / CU-01). Sesión única de escritorio.</summary>
    public static class CtrlAuth
    {
        public static Usuario UsuarioActual { get; private set; }

        public static ResultadoLogin IniciarSesion(string cedula, string clave)
        {
            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrEmpty(clave))
                return Resultado.Falla<ResultadoLogin>("VALIDACION", "Debe ingresar su cédula y contraseña institucional.");

            Usuario usuario = Usuario.Autenticar(cedula, clave);
            if (usuario == null)
                return Resultado.Falla<ResultadoLogin>("AUTH",
                    "Credenciales institucionales inválidas. Verifique su cédula y clave.");

            UsuarioActual = usuario;
            return new ResultadoLogin { Ok = true, Usuario = usuario };
        }

        public static void CerrarSesion()
        {
            UsuarioActual = null;
        }
    }
}
