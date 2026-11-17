using System;
using System.Drawing;
using System.Windows.Forms;
using ReservasUTP.Controllers;
using ReservasUTP.Models;

namespace ReservasUTP.Views
{
    /// <summary>Catálogo de espacios agrupado por categoría (CU-02 / RF-02). Equivale a catalogo_espacios.html.</summary>
    public class FrmCatalogo : Form
    {
        private ListView lvEspacios;
        private Label lblResumen;
        private Label lblAlerta;
        private Label lblDetalle;

        /// <summary>True si el usuario pulsó "Cerrar sesión" (Program vuelve al login).</summary>
        public bool CerroSesion { get; private set; }

        public FrmCatalogo()
        {
            Estilos.AplicarFormulario(this, "Catálogo de espacios", 980, 620);
            MinimumSize = new Size(820, 520);
            ConstruirInterfaz();
            Load += (s, e) => CargarCatalogo();
        }

        private void ConstruirInterfaz()
        {
            var barra = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 8, 12, 0), BackColor = Color.White
            };
            var btnConsultar = Estilos.BotonPrimario("Consultar disponibilidad / Reservar (CU-03, CU-04)");
            btnConsultar.Click += (s, e) => AbrirDisponibilidad();
            var btnMis = Estilos.BotonSecundario("Mis reservas");
            btnMis.Click += (s, e) => { using (var f = new FrmMisReservas()) f.ShowDialog(this); };
            var btnRefrescar = Estilos.BotonSecundario("Actualizar");
            btnRefrescar.Click += (s, e) => CargarCatalogo();
            var btnSalir = Estilos.BotonSecundario("Cerrar sesión");
            btnSalir.Click += (s, e) => { CerroSesion = true; CtrlAuth.CerrarSesion(); Close(); };
            barra.Controls.AddRange(new Control[] { btnConsultar, btnMis, btnRefrescar, btnSalir });

            lblResumen = new Label
            {
                Dock = DockStyle.Top, Height = 30, Padding = new Padding(14, 8, 0, 0),
                Font = Estilos.Negrita, ForeColor = Estilos.Morado
            };
            lblAlerta = Estilos.Alerta();

            lvEspacios = new ListView
            {
                Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true,
                MultiSelect = false, HideSelection = false, Font = Estilos.Normal
            };
            lvEspacios.Columns.Add("Espacio", 260);
            lvEspacios.Columns.Add("Capacidad", 90, HorizontalAlignment.Center);
            lvEspacios.Columns.Add("Equipamiento", 420);
            lvEspacios.Columns.Add("Estado", 110);
            lvEspacios.DoubleClick += (s, e) => AbrirDisponibilidad();
            lvEspacios.SelectedIndexChanged += (s, e) => MostrarDetalle();

            lblDetalle = new Label
            {
                Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(14, 6, 14, 6),
                BackColor = Estilos.MoradoClaro, ForeColor = Color.Black,
                Text = "Seleccione un espacio y pulse \"Consultar disponibilidad\" (o haga doble clic)."
            };

            var contenedor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            contenedor.Controls.Add(lvEspacios);

            Controls.Add(contenedor);
            Controls.Add(lblDetalle);
            Controls.Add(lblAlerta);
            Controls.Add(lblResumen);
            Controls.Add(barra);
            Controls.Add(Estilos.Encabezado("Catálogo de espacios (CU-02)"));
        }

        private void CargarCatalogo()
        {
            lblAlerta.Visible = false;
            lvEspacios.BeginUpdate();
            lvEspacios.Items.Clear();
            lvEspacios.Groups.Clear();

            ResultadoCatalogo r = CtrlConsultarEspacio.MostrarEspacios();
            if (!r.Ok)
            {
                Estilos.MostrarAlerta(lblAlerta, r.Error, Estilos.ErrorFondo, Estilos.OcupadoTexto);
                lvEspacios.EndUpdate();
                return;
            }

            foreach (CategoriaCatalogo cat in r.Categorias)
            {
                var grupo = new ListViewGroup(cat.Nombre,
                    cat.Nombre + "  —  " + cat.Descripcion + "  [" + cat.Restriccion + "]  (" + cat.Espacios.Count + ")");
                lvEspacios.Groups.Add(grupo);
                foreach (Espacio esp in cat.Espacios)
                {
                    var item = new ListViewItem(new[] { esp.Nombre, esp.Capacidad.ToString(), esp.Equipamiento, esp.Estado }, grupo)
                    {
                        Tag = new object[] { esp, cat }
                    };
                    lvEspacios.Items.Add(item);
                }
            }
            lvEspacios.EndUpdate();
            lblResumen.Text = r.TotalEspacios + " espacios habilitados en " + r.Categorias.Count + " categorías";
        }

        private void MostrarDetalle()
        {
            if (lvEspacios.SelectedItems.Count == 0) return;
            var datos = (object[])lvEspacios.SelectedItems[0].Tag;
            var esp = (Espacio)datos[0];
            var cat = (CategoriaCatalogo)datos[1];
            lblDetalle.Text = esp.Nombre + " · " + esp.Categoria + " · Capacidad: " + esp.Capacidad +
                              " personas\r\nRestricción: " + cat.Restriccion;
        }

        private void AbrirDisponibilidad()
        {
            if (lvEspacios.SelectedItems.Count == 0)
            {
                Estilos.MostrarAlerta(lblAlerta, "Seleccione primero un espacio del catálogo.", Estilos.AvisoFondo, Color.Black);
                return;
            }
            var esp = (Espacio)((object[])lvEspacios.SelectedItems[0].Tag)[0];
            using (var f = new FrmDisponibilidad(esp.IdEspacio)) f.ShowDialog(this);
        }
    }
}
