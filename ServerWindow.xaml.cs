using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : Window
    {
        private Socket? listenSocket;  // "слухаючий" сокет - очікує запитів
        private IPEndPoint? endPoint;  // точка, як він "слухає" 
        private LinkedList<ChatMessage> messages;

        public ServerWindow()
        {
            InitializeComponent();
            messages = new();
        }

        private void SwitchServer_Click(object sender, RoutedEventArgs e)
        {
            if(listenSocket == null)  // сервер OFF, включаємо
            {
                try
                {
                    // парсимо хост - визначаємо номер-адрес вузла з текстового вигляду
                    IPAddress ip = IPAddress.Parse(HostTextBox.Text);
                    // те ж саме з портом
                    int port = Convert.ToInt32(PortTextBox.Text);
                    // збираємо хост + порт в endpoint
                    endPoint = new(ip, port);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(
                        "Неправильні параметри конфігурації: " 
                        + ex.Message);
                    return;
                }
                listenSocket = new Socket(
                    AddressFamily.InterNetwork,  // IPv4
                    SocketType.Stream,           // Двосторонній (читає та пише)
                    ProtocolType.Tcp);           // Протокол - TCP
                // Стартуємо сервер. Оскільки процес слухання довгий (нескінченний)
                // запускаємо його в окремому потоці
                new Thread(StartServer).Start();
            }
            else  // сервер ON, виключаємо
            {
                // сервер зупинити, якщо він в очікуванні, дуже складно
                listenSocket.Close();  // створюємо конфлікт - закриваємо сокет
                // це призведе до exception у потоці сервера 
            }
        }

        private void StartServer()
        {
            if(listenSocket == null || endPoint == null)
            {
                MessageBox.Show("Спроба запуску без ініціалізації даних ");
                return;
            }

            try
            {
                listenSocket.Bind(endPoint);  // якщо порт зайнятий, то буде виключення
                listenSocket.Listen(10);      // 10 запитів - максимальна черга (іншим-відмова)
                Dispatcher.Invoke(() => ServerLog.Text += "Сервер запущен\n");

                byte[] buffer = new byte[1024];  // буфер прийому даних
                while(true)  // нескінченний процес слухання - постійна робота сервера
                {
                    // очікування запиту, саме ця інструкція блокує потік до надходження запиту
                    Socket socket = listenSocket.Accept();

                    // цей код виконується коли сервер отримав запит
                    /* // небажано - збирати рядки з фрагментів байт-послідовності,
                     * оскільки різні символи мають різну байтову довжину і фрагмент
                     * може містити неповний символ (через обмеженість буфера)
                     * Також у JSON деякі символи кодуються типу \u2311, які теж
                     * не бажано розривати
                    StringBuilder stringBuilder = new();
                    do
                    {
                        int n = socket.Receive(buffer);   // отримаємо пакет, зберігаємо 
                        // у буфері. n - кількість реально отриманих байт
                        // Декодуємо одержані байти у рядок та додаємо до stringBuilder
                        stringBuilder.Append(
                            Encoding.UTF8.GetString(buffer, 0, n));  // TODO: визначити кодування з налаштувань

                    } while (socket.Available > 0);   // повторюємо цикл доки у сокеті є дані
                    String str = stringBuilder.ToString();
                    */
                    MemoryStream memoryStream = new();   // "ByteBuilder" - спосіб накопичити байти
                    do
                    {
                        int n = socket.Receive(buffer);
                        memoryStream.Write(buffer, 0, n);
                    } while (socket.Available > 0);
                    String str = Encoding.UTF8.GetString(memoryStream.ToArray());
                    // декодуємо з JSON, знаючи, що це ClientRequest
                    ServerResponse serverResponse = new();
                    ClientRequest? clientRequest = null;
                    try { clientRequest = JsonSerializer.Deserialize<ClientRequest>(str); }
                    catch { }
                    bool needLog = true;
                    if (clientRequest == null)
                    {
                        str = "Error decoding JSON: " + str;
                        serverResponse.Status = "400 Bad request";
                        // serverResponse.Data = "Error decoding JSON";
                    }
                    else  // запит декодований, визначаємо команду запиту
                    {
                        if (clientRequest.Command.Equals("Message"))
                        {
                            // час встановлюємо на сервері
                            clientRequest.Message.Moment = DateTime.Now;
                            // додаємо до колекції
                            messages.AddLast(clientRequest.Message);
                            // логуємо
                            str = clientRequest.Message.ToString();
                            serverResponse.Status = "200 OK";
                            // serverResponse.Data = "Received " + clientRequest.Message.Moment;
                        }
                        else if (clientRequest.Command.Equals("Check"))
                        {
                            // визначаємо момент останньої синхронізації
                            // та надсилаємо у відповідь всі повідомлення, пізніші цього моменту
                            serverResponse.Status = "200 OK";
                            serverResponse.Messages =
                                messages.Where(m => m.Moment > clientRequest.Message.Moment);
                            needLog = false;
                        }
                    }
                    if (needLog)
                    {
                        Dispatcher.Invoke(() => ServerLog.Text += $"{DateTime.Now} {str}\n");
                    }
                    // Сервер готує відповідь і надсилає клієнту
                    socket.Send(Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(serverResponse)
                    ));

                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                // Імовірніше за все сервер зупинився кнопкою з UI
                // У будь-якому разі роботу припинено, зануляємо посилання
                listenSocket = null;
                Dispatcher.Invoke(() => ServerLog.Text += "Сервер зупинено\n");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // !! Не забувати зупиняти сервер при закритті вікна
            listenSocket?.Close();
        }
    }
}
/* Т.З. Реалізувати обмін повідомленнями:
 * клієнти періодично надсилають на сервер запити на оновлення
 * повідомлень, якщо такі є, то сервер надсилає масив нових даних,
 * інакше - "порожню" відповідь
 * Самі повідомлення надсилаються окремими видом запитів.
 * клієнт 1        сервер       клієнт 3         
 * Вітання ----->         <---- check   
 *                        --> [ клієнт 1:Вітання, клієнт 2:Hello ] 
 * клієнт 2       
 * Hello  -----> 
 * 
 * Розгляд:
 * Як серверу вирішувати, кому і що передавати
 * а) все на сервері - погано для великої кількості користувачів
 * б) кожен клієнт сам передає дані про останню синхронізацію
 * У якості "мітки" синхронізації будемо використовувати час
 */
