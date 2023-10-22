using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NetworkProgrammingP12
{
    /// <summary>
    /// Логика взаимодействия для HttpWindow.xaml
    /// </summary>
    public partial class HttpWindow : Window
    {
        public string[] popularCc = {"EUR", "USD", "XAU"};
        public HttpWindow()
        {
            InitializeComponent();
        }

        private async void get1Button_Click(object sender, RoutedEventArgs e)
        {
            using HttpClient  httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://itstep.org");
            textBlock1.Text = "";
            textBlock1.Text += (int)response.StatusCode + " " + response.ReasonPhrase + "\r\n";
            foreach (var header in response.Headers)
            {
                textBlock1.Text += $"{header.Key,20}: "
                                + String.Join(',', header.Value).Ellipsis(30) + "\r\n";
            }
            string body = await response.Content.ReadAsStringAsync();
            textBlock1.Text += $"\r\n{body}";
        }

        private async void ratesButton_Click(object sender, RoutedEventArgs e)
        {
            using HttpClient httpClient = new();
            string body = await httpClient.GetStringAsync(@"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json");
            var rates = JsonSerializer.Deserialize<List<NbuRate>>(body);
            if (rates == null)
            {
                MessageBox.Show("Error deserializing");
                return;
            }
            foreach (var rate in rates)
            {
                textBlock1.Text += $"{rate.cc} {rate.txt} {rate.rate}\n";
            }
        }

        private async void sortRatesButton_Click(object sender, RoutedEventArgs e)
        {
            using HttpClient httpClient = new();
            string body = await httpClient.GetStringAsync(@"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json");
            var rates = JsonSerializer.Deserialize<List<NbuRate>>(body);
            if (rates == null)
            {
                MessageBox.Show("Error deserializing");
                return;
            }
            foreach (var rate in rates)
            {
                if (popularCc.Contains(rate.cc))
                textBlock1.Text += $"{rate.cc} {rate.txt} {rate.rate}\n";
            }
        }

        private async void RatexmlBtn_Click(object sender, RoutedEventArgs e)
        {
            string xmlUrl = @"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange";

            using HttpClient httpClient = new HttpClient();

            string xmlContent = await httpClient.GetStringAsync(xmlUrl);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            string xmlText = "";


            XmlNodeList rateNodes = xmlDoc.SelectNodes("exchange/currency");

            foreach (XmlNode rateNode in rateNodes)
            {
                string cc = rateNode.SelectSingleNode("cc").InnerText;
                string txt = rateNode.SelectSingleNode("txt").InnerText;
                string rate = rateNode.SelectSingleNode("rate").InnerText;

                xmlText += $"{cc} {txt} {rate}\n";
            }

            textBlock1.Text = xmlText;
        }
    }

    class NbuRate
    {
        public int r030 { get; set; }
        public string txt {  get; set; }
        public double rate { get; set; }
        public string cc {  get; set; }
        public string exchangedate {  get; set; }
    }

    public static class EllipsisExtensions
    {
        public static string Ellipsis(this string str, int maxLength)
        {
            return str.Length > maxLength ? str[..(maxLength - 3)] + "..." : str;
        }
    }
}
