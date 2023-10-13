using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetworkProgrammingP12
{
    /// <summary>
    /// Логика взаимодействия для EmailWindow.xaml
    /// </summary>
    public partial class EmailWindow : Window
    {
        public EmailWindow()
        {
            InitializeComponent();
        }

        private void SendButton1_Click(object sender, RoutedEventArgs e)
        {
            #region config
            string host = App.GetConfiguration("smtp:host");
            if (host == "Error")
            {
                MessageBox.Show("JSON File error. Check your file",
                    "File error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }
            if (host == null)
            {
                MessageBox.Show("Error getting host");
                return;
            }
            string portString = App.GetConfiguration("smtp:port");
            if (portString == null)
            {
                MessageBox.Show("Error getting port");
                return;
            }
            int port;
            try { port = int.Parse(portString); }
            catch { MessageBox.Show("Error parsing port"); return; }
            string? email = App.GetConfiguration("smtp:email");
            if (email == null)
            {
                MessageBox.Show("Error getting email");
                return;
            }
            string? password = App.GetConfiguration("smtp:password");
            if (password == null)
            {
                MessageBox.Show("Error getting password");
                return;
            }
            string? sslString = App.GetConfiguration("smtp:ssl");
            if (sslString == null)
            {
                MessageBox.Show("Error getting ssl");
                return;
            }
            bool ssl;
            try { ssl = bool.Parse(sslString); }
            catch
            {
                MessageBox.Show("Error parsing ssl");
                return;
            }
            #endregion
            if (!textBoxTo.Text.Contains("@"))
            {
                MessageBox.Show("Invalid email, try again");
                return;
            }
            using SmtpClient smtpClient = new(host, port)
            {
                EnableSsl = ssl,
                Credentials = new NetworkCredential(email, password)
            };
            smtpClient.Send(
                email,
                textBoxTo.Text,
                textBoxSubject.Text,
                textBoxMessage.Text
            );
            MessageBox.Show("Sended");
        }
    }
}
