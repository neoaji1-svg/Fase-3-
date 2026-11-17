using System.Drawing;
using System.Windows.Forms;
using ReservasUTP.Controllers;

namespace ReservasUTP.Views
{
    /// <summary>Comprobante de reserva exitosa (CU-04 paso 12-13 / RF-08). Equivale a confirmar_reserva.html.</summary>
    public class FrmConfirmacion : Form
    {
        public FrmConfirmacion(ResultadoReserva r)
        {
            Estilos.AplicarFormulario(this, "Reserva confirmada", 520, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var banda = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Estilos.ExitoFondo };
            banda.Controls.Add(new Label
            {
                Text = "✔ Reserva Realizada con Éxito", Font = Estilos.Titulo, ForeColor = Estilos.DisponibleTexto,
                AutoSize = true, Location = new Point(20, 18)
            });

            var tabla = new TableLayoutPanel
            {
                Location = new Point(24, 80), Size = new Size(470, 190), ColumnCount = 2, BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Fila(tabla, "N.º de reserva", "#" + r.IdReserva);
            Fila(tabla, "Usuario", r.Usuario.Nombre + " (" + r.Usuario.Rol + ")");
            Fila(tabla, "Espacio", r.Espacio.Nombre);
            Fila(tabla, "Categoría", r.Espacio.Categoria);
            Fila(tabla, "Fecha", r.Fecha.ToString("yyyy-MM-dd"));
            Fila(tabla, "Horario", r.HoraInicio.ToString(@"hh\:mm") + " - " + r.HoraFin.ToString(@"hh\:mm"));
            Fila(tabla, "Estado", "Activa");

            var aviso = new Label
            {
                Location = new Point(24, 280), Size = new Size(470, 50),
                Text = (r.NotificacionEnviada ? "📧 " : "⚠ ") + r.AvisoCorreo +
                       "\r\nTiempo de procesamiento: " + r.TiempoMs.ToString("0.0") + " ms (RNF-01 < 3000 ms)",
                BackColor = r.NotificacionEnviada ? Estilos.ExitoFondo : Estilos.AvisoFondo,
                Padding = new Padding(6)
            };

            var btn = Estilos.BotonPrimario("Aceptar");
            btn.Location = new Point(410, 345);
            btn.DialogResult = DialogResult.OK;
            AcceptButton = btn;

            Controls.AddRange(new Control[] { banda, tabla, aviso, btn });
        }

        private static void Fila(TableLayoutPanel t, string campo, string valor)
        {
            t.Controls.Add(new Label { Text = campo, Font = Estilos.Negrita, AutoSize = true, Margin = new Padding(6, 4, 6, 4) });
            t.Controls.Add(new Label { Text = valor, AutoSize = true, Margin = new Padding(6, 4, 6, 4) });
        }
    }
}
