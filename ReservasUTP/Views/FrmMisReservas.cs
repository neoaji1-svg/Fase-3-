using System;
using System.Drawing;
using System.Windows.Forms;
using ReservasUTP.Controllers;
using ReservasUTP.Models;

namespace ReservasUTP.Views
{
    /// <summary>Historial de reservas del usuario en sesión (apoyo a evidencia de CU-04). Equivale a mis_reservas.html.</summary>
    public class FrmMisReservas : Form
    {
        public FrmMisReservas()
        {
            Estilos.AplicarFormulario(this, "Mis reservas", 900, 480);
            StartPosition = FormStartPosition.CenterParent;

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Estilos.Morado;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = Estilos.Negrita;
            grid.Columns.Add("id", "N.º");
            grid.Columns.Add("espacio", "Espacio");
            grid.Columns.Add("categoria", "Categoría");
            grid.Columns.Add("fecha", "Fecha");
            grid.Columns.Add("horario", "Horario");
            grid.Columns.Add("estado", "Estado");
            grid.Columns.Add("registro", "Registrada el");
            grid.Columns[0].FillWeight = 40;

            var alerta = Estilos.Alerta();
            var cont = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            cont.Controls.Add(grid);
            Controls.Add(cont);
            Controls.Add(alerta);
            Controls.Add(Estilos.Encabezado("Mis reservas"));

            try
            {
                var lista = Reserva.ObtenerMisReservas(CtrlAuth.UsuarioActual.IdUsuario);
                foreach (Reserva r in lista)
                {
                    grid.Rows.Add(r.IdReserva, r.EspacioNombre, r.Categoria, r.Fecha.ToString("yyyy-MM-dd"),
                        r.HoraInicio.ToString(@"hh\:mm") + " - " + r.HoraFin.ToString(@"hh\:mm"), r.Estado,
                        r.FechaRegistro.ToString("yyyy-MM-dd HH:mm"));
                }
                if (lista.Count == 0)
                    Estilos.MostrarAlerta(alerta, "Aún no tiene reservas registradas.", Estilos.AvisoFondo, Color.Black);
            }
            catch (Exception ex)
            {
                Estilos.MostrarAlerta(alerta, "No fue posible cargar sus reservas: " + ex.Message,
                    Estilos.ErrorFondo, Estilos.OcupadoTexto);
            }
        }
    }
}
