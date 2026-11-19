using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RealVisionLab
{
    public partial class PasswordForm : Form
    {
        public bool isTrue = false;
        bool newPass = false;

        public PasswordForm()
        {
            InitializeComponent();
        }

        private void PasswordForm_Load(object sender, EventArgs e)
        {
            txt_password.Focus();
            isTrue = false;
            if (Security.Credentials.NeedsInitialSetup())
            {
                // First use: no password exists yet, ask for one.
                newPass = true;
                label2.ForeColor = Color.Orange;
                label2.Text = "İlk kullanım: yeni şifre belirleyin (4 haneli)";
            }
        }

        private async void txt_password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (newPass)
                {
                    newPass = false;
                    if (txt_password.Text == null || txt_password.TextLength != 4)
                    {
                        label2.ForeColor = Color.Red;
                        label2.Text = "Şifre boş olmamalı ve 4 haneli olmalıdır.";
                        await Task.Delay(1500);
                        Close();
                    }
                    else
                    {
                        Security.Credentials.SetUserPassword(txt_password.Text.Trim());
                        label2.ForeColor = Color.Green;
                        label2.Text = "Şifre Başarıyla Güncellendi";
                        await Task.Delay(1500);
                        Close();
                    }

                }
                else
                {
                    string text = txt_password.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        label2.ForeColor = Color.Red;
                        label2.Text = "Şifre boş olmamalıdır.";
                        await Task.Delay(1500);
                        Close();
                    }
                    else if (
                            text.Length > 2 &&
                            text.EndsWith("*+") &&
                                (
                                Security.Credentials.VerifyUser(text.Substring(0, text.Length - 2)) ||
                                Security.Credentials.VerifyServiceCode(text.Substring(0, text.Length - 2))
                                )
                            )
                    {
                        newPass = true;
                        txt_password.Clear();
                        label2.Text = "Yeni Şireyi Girin";
                        label2.ForeColor = Color.Orange;
                    }
                    else
                    {
                        if (Security.Credentials.VerifyUser(text.Trim()) || Security.Credentials.VerifyServiceCode(text.Trim()))
                        {
                            label2.ForeColor = Color.Green;
                            label2.Text = "Şifre doğru";
                            isTrue = true;
                            await Task.Delay(1500);
                            Close();
                        }
                        else
                        {
                            label2.ForeColor = Color.Red;
                            label2.Text = "Şifre yanlış";
                            isTrue = false;
                            await Task.Delay(1500);
                            Close();
                        }
                    }
                }
            }
        }
    }
}
