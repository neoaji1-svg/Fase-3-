using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ReservasUTP.Config;
using ReservasUTP.Controllers;
using ReservasUTP.Models;

namespace ReservasUTP.Views
{
    /// <summary>
    /// Consulta de disponibilidad (CU-03 / RF-03) + solicitud de reserva (CU-04 / RF-04).
    /// Equivale a views/consultar_horario.html.
    /// </summary>
    public class FrmDisponibilidad : Form
    {
        public static readonly DateTime FechaDemo = new DateTime(2026, 10, 19);

        private ComboBox cmbEspacio;
        private DateTimePicker dtpFecha;
        private ComboBox cmbVista;
        private CheckBox chkErrorBd;
        private Label lblInfo;
        private Label lblAlerta;
        private DataGridView grid;

        private TextBox txtEspacioSel;
        private TextBox txtFechaSel;
        private ComboBox cmbHoraInicio;
        private ComboBox cmbHoraFin;
        private CheckBox chkFalloCorreo;
        private Button btnConfirmar;

        private ResultadoDisponibilidad _ultimo;
        private Espacio _espacioReserva;
        private DateTime _fechaReserva;
        private bool _cargando;

        public FrmDisponibilidad(int idEspacioInicial)
        {
            Estilos.AplicarFormulario(this, "Disponibilidad y reserva", 1100, 760);
            MinimumSize = new Size(900, 640);
            ConstruirInterfaz();
            CargarEspacios(idEspacioInicial);
            Load += (s, e) => Consultar();
        }

        private void ConstruirInterfaz()
        {
            // ---- Filtros CU-03 (FA-01 vista, FA-02 cambio de espacio) ----
            var filtros = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 64, Padding = new Padding(12, 6, 12, 0), BackColor = Color.White,
                WrapContents = false
            };
            cmbEspacio = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            dtpFecha = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120, Value = FechaDemo };
            cmbVista = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
            cmbVista.Items.AddRange(new object[] { "Diaria", "Semanal" });
            cmbVista.SelectedIndex = 0;
            var btnActualizar = Estilos.BotonPrimario("Actualizar disponibilidad");
            btnActualizar.Click += (s, e) => Consultar();
            chkErrorBd = new CheckBox { Text = "Simular fallo de BD (EX-02)", AutoSize = true, Margin = new Padding(10, 26, 0, 0) };

            filtros.Controls.Add(Campo("1. Espacio / Salón (FA-02):", cmbEspacio));
            filtros.Controls.Add(Campo("2. Fecha:", dtpFecha));
            filtros.Controls.Add(Campo("3. Vista (FA-01):", cmbVista));
            btnActualizar.Margin = new Padding(8, 20, 0, 0);
            filtros.Controls.Add(btnActualizar);
            filtros.Controls.Add(chkErrorBd);

            cmbEspacio.SelectedIndexChanged += (s, e) => { if (!_cargando) Consultar(); };
            cmbVista.SelectedIndexChanged += (s, e) => { if (!_cargando) Consultar(); };
            dtpFecha.CloseUp += (s, e) => Consultar();

            lblInfo = new Label
            {
                Dock = DockStyle.Top, Height = 46, Padding = new Padding(14, 6, 14, 0), ForeColor = Estilos.TextoSecundario
            };
            lblAlerta = Estilos.Alerta();

            // ---- Cuadrícula de disponibilidad ----
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Estilos.Morado;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = Estilos.Negrita;
            grid.ColumnHeadersHeight = 34;
            grid.RowTemplate.Height = 30;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.CellClick += Grid_CellClick;

            var contGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
            contGrid.Controls.Add(grid);

            // ---- Formulario CU-04 ----
            var grpReserva = new GroupBox
            {
                Text = "Solicitar y confirmar reserva (CU-04)", Dock = DockStyle.Bottom, Height = 150,
                Font = Estilos.Negrita, ForeColor = Estilos.Morado, Padding = new Padding(10)
            };
            var panelRes = new FlowLayoutPanel { Dock = DockStyle.Fill, Font = Estilos.Normal, ForeColor = Color.Black };
            txtEspacioSel = new TextBox { ReadOnly = true, Width = 260 };
            txtFechaSel = new TextBox { ReadOnly = true, Width = 110 };
            cmbHoraInicio = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
            cmbHoraFin = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };
            foreach (BloqueHorario b in AppConfig.BloquesHorarios)
            {
                cmbHoraInicio.Items.Add(b.Inicio.ToString(@"hh\:mm"));
                cmbHoraFin.Items.Add(b.Fin.ToString(@"hh\:mm"));
            }
            cmbHoraInicio.SelectedIndex = 0;
            cmbHoraFin.SelectedIndex = 0;
            panelRes.Controls.Add(Campo("Espacio seleccionado:", txtEspacioSel));
            panelRes.Controls.Add(Campo("Fecha:", txtFechaSel));
            panelRes.Controls.Add(Campo("Hora de inicio:", cmbHoraInicio));
            panelRes.Controls.Add(Campo("Hora de fin:", cmbHoraFin));

            chkFalloCorreo = new CheckBox
            {
                Text = "Simular fallo en servicio SMTP (EX-04, no bloqueante)", AutoSize = true, Margin = new Padding(6, 10, 20, 0)
            };
            btnConfirmar = Estilos.BotonPrimario("Confirmar reserva");
            btnConfirmar.Margin = new Padding(6, 4, 0, 0);
            btnConfirmar.Click += BtnConfirmar_Click;
            panelRes.SetFlowBreak(panelRes.Controls[panelRes.Controls.Count - 1], true);
            panelRes.Controls.Add(chkFalloCorreo);
            panelRes.Controls.Add(btnConfirmar);
            panelRes.Controls.Add(new Label
            {
                Text = "Tip: haga clic en una celda \"Disponible\" para precargar espacio, fecha y horario.",
                AutoSize = true, ForeColor = Estilos.TextoSecundario, Margin = new Padding(12, 10, 0, 0)
            });
            grpReserva.Controls.Add(panelRes);

            var btnCerrar = Estilos.BotonSecundario("← Volver al catálogo (CU-02)");
            btnCerrar.Click += (s, e) => Close();
            var pie = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(12, 4, 12, 0) };
            pie.Controls.Add(btnCerrar);

            Controls.Add(contGrid);
            Controls.Add(grpReserva);
            Controls.Add(pie);
            Controls.Add(lblAlerta);
            Controls.Add(lblInfo);
            Controls.Add(filtros);
            Controls.Add(Estilos.Encabezado("Disponibilidad (CU-03) y Reserva (CU-04)"));
        }

        private static Control Campo(string etiqueta, Control control)
        {
            var p = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 12, 0)
            };
            p.Controls.Add(new Label { Text = etiqueta, AutoSize = true });
            p.Controls.Add(control);
            return p;
        }

        private void CargarEspacios(int idInicial)
        {
            _cargando = true;
            try
            {
                // Se listan todos (incluidos inhabilitados) para poder evidenciar EX-01 desde la interfaz.
                foreach (Espacio e in Espacio.ObtenerTodos())
                {
                    cmbEspacio.Items.Add(new ItemEspacio(e));
                    if (e.IdEspacio == idInicial) cmbEspacio.SelectedIndex = cmbEspacio.Items.Count - 1;
                }
                if (cmbEspacio.SelectedIndex < 0 && cmbEspacio.Items.Count > 0) cmbEspacio.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Estilos.MostrarAlerta(lblAlerta, "No fue posible cargar los espacios: " + ex.Message,
                    Estilos.ErrorFondo, Estilos.OcupadoTexto);
            }
            finally
            {
                _cargando = false;
            }
        }

        private void Consultar()
        {
            var item = cmbEspacio.SelectedItem as ItemEspacio;
            if (item == null) return;

            lblAlerta.Visible = false;
            string vista = cmbVista.SelectedIndex == 1 ? CtrlConsultarHorario.VistaSemanal : CtrlConsultarHorario.VistaDiaria;
            Cursor = Cursors.WaitCursor;
            _ultimo = CtrlConsultarHorario.ConsultarDisponibilidad(item.Espacio.IdEspacio, dtpFecha.Value.Date, vista, chkErrorBd.Checked);
            Cursor = Cursors.Default;

            grid.Columns.Clear();
            grid.Rows.Clear();

            if (!_ultimo.Ok)
            {
                Estilos.MostrarAlerta(lblAlerta, _ultimo.Error, Estilos.ErrorFondo, Estilos.OcupadoTexto);
                lblInfo.Text = item.Espacio.Nombre + " · " + item.Espacio.Categoria;
                btnConfirmar.Enabled = false;
                txtEspacioSel.Text = "";
                txtFechaSel.Text = "";
                _espacioReserva = null;
                return;
            }

            btnConfirmar.Enabled = true;
            Espacio esp = _ultimo.Espacio;
            lblInfo.Text = "Categoría: " + esp.Categoria + "  |  Capacidad: " + esp.Capacidad + " personas\r\n" +
                           "Equipamiento: " + esp.Equipamiento;

            grid.Columns.Add("bloque", "Bloque horario");
            grid.Columns[0].FillWeight = 70;
            for (int i = 0; i < _ultimo.Columnas.Count; i++)
                grid.Columns.Add("c" + i, _ultimo.Columnas[i]);
            foreach (DataGridViewColumn c in grid.Columns) c.SortMode = DataGridViewColumnSortMode.NotSortable;

            foreach (FilaDisponibilidad fila in _ultimo.Matriz)
            {
                int idx = grid.Rows.Add();
                DataGridViewRow row = grid.Rows[idx];
                row.Cells[0].Value = fila.Bloque;
                row.Cells[0].Style.Font = Estilos.Negrita;
                for (int i = 0; i < fila.Celdas.Count; i++)
                {
                    CeldaDisponibilidad celda = fila.Celdas[i];
                    DataGridViewCell c = row.Cells[i + 1];
                    c.Value = celda.Estado;
                    c.Tag = celda;
                    c.Style.BackColor = celda.Ocupado ? Estilos.Ocupado : Estilos.Disponible;
                    c.Style.ForeColor = celda.Ocupado ? Estilos.OcupadoTexto : Estilos.DisponibleTexto;
                    c.Style.SelectionBackColor = celda.Ocupado ? Color.IndianRed : Estilos.Morado;
                }
            }
            grid.ClearSelection();

            // Valores por defecto del formulario CU-04
            SeleccionarParaReserva(esp, _ultimo.Fecha, null);
        }

        private void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 1 || _ultimo == null || !_ultimo.Ok) return;
            var celda = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag as CeldaDisponibilidad;
            if (celda == null) return;
            if (celda.Ocupado)
            {
                Estilos.MostrarAlerta(lblAlerta, "Ese bloque ya está ocupado. Seleccione un bloque disponible.",
                    Estilos.AvisoFondo, Color.Black);
                return;
            }
            lblAlerta.Visible = false;
            Espacio esp = _ultimo.EspaciosCategoria.FirstOrDefault(x => x.IdEspacio == celda.IdEspacio) ?? _ultimo.Espacio;
            SeleccionarParaReserva(esp, celda.Fecha, _ultimo.Matriz[e.RowIndex]);
        }

        private void SeleccionarParaReserva(Espacio esp, DateTime fecha, FilaDisponibilidad fila)
        {
            _espacioReserva = esp;
            _fechaReserva = fecha;
            txtEspacioSel.Text = esp.Nombre + " (" + esp.Categoria + ")";
            txtFechaSel.Text = fecha.ToString("yyyy-MM-dd");
            if (fila != null)
            {
                cmbHoraInicio.SelectedItem = fila.HoraInicio.ToString(@"hh\:mm");
                cmbHoraFin.SelectedItem = fila.HoraFin.ToString(@"hh\:mm");
            }
        }

        private void BtnConfirmar_Click(object sender, EventArgs e)
        {
            if (_espacioReserva == null || CtrlAuth.UsuarioActual == null) return;

            TimeSpan hi = TimeSpan.Parse((string)cmbHoraInicio.SelectedItem);
            TimeSpan hf = TimeSpan.Parse((string)cmbHoraFin.SelectedItem);

            var confirmar = MessageBox.Show(this,
                string.Format("¿Confirmar la reserva de '{0}' el {1:yyyy-MM-dd} de {2:hh\\:mm} a {3:hh\\:mm}?",
                    _espacioReserva.Nombre, _fechaReserva, hi, hf),
                "Confirmar reserva", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            ResultadoReserva r = CtrlCrearReserva.ProcesarReserva(CtrlAuth.UsuarioActual.IdUsuario,
                _espacioReserva.IdEspacio, _fechaReserva, hi, hf, chkFalloCorreo.Checked);
            Cursor = Cursors.Default;

            if (!r.Ok)
            {
                Estilos.MostrarAlerta(lblAlerta, r.Error, Estilos.ErrorFondo, Estilos.OcupadoTexto);
                return;
            }

            using (var f = new FrmConfirmacion(r)) f.ShowDialog(this);
            Consultar();
        }

        /// <summary>Elemento del combo de espacios (muestra el estado si está inhabilitado).</summary>
        private class ItemEspacio
        {
            public Espacio Espacio { get; private set; }
            public ItemEspacio(Espacio e) { Espacio = e; }
            public override string ToString()
            {
                return Espacio.Nombre + " — " + Espacio.Categoria + (Espacio.Estado != "Habilitado" ? "  [" + Espacio.Estado + "]" : "");
            }
        }
    }
}
