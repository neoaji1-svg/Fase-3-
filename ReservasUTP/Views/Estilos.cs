using System.Drawing;
using System.Windows.Forms;
using ReservasUTP.Controllers;

namespace ReservasUTP.Views
{
    /// <summary>Paleta y helpers visuales compartidos (equivalente a static/css/estilos.css).</summary>
    internal static class Estilos
    {
        public static readonly Color Morado = Color.FromArgb(91, 44, 131);
        public static readonly Color MoradoClaro = Color.FromArgb(237, 231, 246);
        public static readonly Color Fondo = Color.FromArgb(245, 246, 250);
        public static readonly Color TextoSecundario = Color.FromArgb(100, 100, 110);
        public static readonly Color Disponible = Color.FromArgb(200, 237, 205);
        public static readonly Color DisponibleTexto = Color.FromArgb(27, 94, 32);
        public static readonly Color Ocupado = Color.FromArgb(255, 205, 210);
        public static readonly Color OcupadoTexto = Color.FromArgb(183, 28, 28);
        public static readonly Color ErrorFondo = Color.FromArgb(255, 235, 238);
        public static readonly Color ExitoFondo = Color.FromArgb(232, 245, 233);
        public static readonly Color AvisoFondo = Color.FromArgb(255, 248, 225);

        public static readonly Font Normal = new Font("Segoe UI", 9.5f);
        public static readonly Font Negrita = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font Titulo = new Font("Segoe UI", 14f, FontStyle.Bold);
        public static readonly Font Subtitulo = new Font("Segoe UI", 11f, FontStyle.Bold);

        public static void AplicarFormulario(Form f, string titulo, int ancho, int alto)
        {
            f.Text = titulo + " - Reservas UTP";
            f.Font = Normal;
            f.BackColor = Fondo;
            f.ClientSize = new Size(ancho, alto);
            f.StartPosition = FormStartPosition.CenterScreen;
        }

        public static Button BotonPrimario(string texto)
        {
            var b = new Button
            {
                Text = texto,
                BackColor = Morado,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Negrita,
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static Button BotonSecundario(string texto)
        {
            var b = new Button
            {
                Text = texto,
                BackColor = Color.White,
                ForeColor = Morado,
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Morado;
            return b;
        }

        /// <summary>Barra superior con el nombre del sistema y el usuario en sesión (navbar).</summary>
        public static Panel Encabezado(string seccion)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Morado };
            var titulo = new Label
            {
                Text = "Reservas UTP · FISC  |  " + seccion,
                ForeColor = Color.White,
                Font = Subtitulo,
                AutoSize = true,
                Location = new Point(16, 17)
            };
            panel.Controls.Add(titulo);

            var u = CtrlAuth.UsuarioActual;
            if (u != null)
            {
                var usuario = new Label
                {
                    Text = u.Nombre + "  (" + u.Rol + ")",
                    ForeColor = Color.White,
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                panel.Controls.Add(usuario);
                panel.Resize += (s, e) => usuario.Location = new Point(panel.Width - usuario.Width - 16, 20);
            }
            return panel;
        }

        /// <summary>Etiqueta de alerta (error / éxito / aviso), oculta por defecto.</summary>
        public static Label Alerta()
        {
            return new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 44,
                Padding = new Padding(12, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false,
                Font = Negrita
            };
        }

        public static void MostrarAlerta(Label alerta, string texto, Color fondo, Color textoColor)
        {
            alerta.Text = texto;
            alerta.BackColor = fondo;
            alerta.ForeColor = textoColor;
            alerta.Visible = true;
        }
    }
}
