using System;
using System.Drawing;
using System.Windows.Forms;
using ReservasUTP.Controllers;

namespace ReservasUTP.Views
{
    /// <summary>Pantalla de inicio de sesión (CU-01 / RF-01). Equivale a views/login.html.</summary>
    public class FrmLogin : Form
    {
        private TextBox txtCedula;
        private TextBox txtClave;
        private Label lblError;
        private Button btnIngresar;

        public FrmLogin()
        {
            Estilos.AplicarFormulario(this, "Iniciar sesión", 420, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ConstruirInterfaz();
        }

        private void ConstruirInterfaz()
        {
            var banda = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Estilos.Morado };
            banda.Controls.Add(new Label
            {
                Text = "Sistema de Reservas de Espacios",
                ForeColor = Color.White, Font = Estilos.Titulo, AutoSize = true, Location = new Point(24, 18)
            });
            banda.Controls.Add(new Label
            {
                Text = "Universidad Tecnológica de Panamá · FISC",
                ForeColor = Color.White, AutoSize = true, Location = new Point(26, 54)
            });

            var lblCedula = new Label { Text = "Cédula institucional:", AutoSize = true, Location = new Point(40, 115) };
            txtCedula = new TextBox { Location = new Point(40, 137), Width = 340 };

            var lblClave = new Label { Text = "Contraseña:", AutoSize = true, Location = new Point(40, 175) };
            txtClave = new TextBox { Location = new Point(40, 197), Width = 340, UseSystemPasswordChar = true };

            lblError = new Label
            {
                Location = new Point(40, 232), Size = new Size(340, 40),
                ForeColor = Estilos.OcupadoTexto, Visible = false
            };

            btnIngresar = Estilos.BotonPrimario("Iniciar sesión");
            btnIngresar.AutoSize = false;
            btnIngresar.Size = new Size(340, 38);
            btnIngresar.Location = new Point(40, 278);
            btnIngresar.Click += BtnIngresar_Click;

            var lblDemo = new Label
            {
                Text = "Demo: 1-1-1 / utp2026  ·  8-1028-886 / tesista2026",
                ForeColor = Estilos.TextoSecundario, AutoSize = true, Location = new Point(40, 335)
            };

            Controls.AddRange(new Control[] { banda, lblCedula, txtCedula, lblClave, txtClave, lblError, btnIngresar, lblDemo });
            AcceptButton = btnIngresar;
        }

        private void BtnIngresar_Click(object sender, EventArgs e)
        {
            lblError.Visible = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                ResultadoLogin r = CtrlAuth.IniciarSesion(txtCedula.Text, txtClave.Text);
                if (!r.Ok)
                {
                    lblError.Text = r.Error;
                    lblError.Visible = true;
                    txtClave.Clear();
                    txtClave.Focus();
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = "No se pudo conectar con la base de datos: " + ex.Message;
                lblError.Visible = true;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
