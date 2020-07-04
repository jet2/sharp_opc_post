using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading;

namespace Siemens.Opc.DaClient
{
    class ServerTCP
    {
        const int port = 8888; // порт для прослушивания подключений
        public ServerTCP(Queue TCPServerQ)
        {
            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("0.0.0.0");
                server = new TcpListener(localAddr, port);

                // запуск слушателя
                server.Start();

                while (true)
                {
                    // получаем входящее подключение
                    TcpClient client = server.AcceptTcpClient();
                    // получаем сетевой поток для чтения и записи
                    NetworkStream stream = client.GetStream();

                    byte[] data = new byte[256];
                    StringBuilder receivedData = new StringBuilder();

                    do
                    {
                        int byteslen = stream.Read(data, 0, data.Length);
                        receivedData.Append(Encoding.UTF8.GetString(data, 0, byteslen));
                    }
                    while (stream.DataAvailable); // пока данные есть в потоке

                    // длиня полученного минус 8 байт заголовка в HEX
                    string xlen = (receivedData.Length -8).ToString("X8");
                    // отправка сообщения c преобразованием сообщения в массив байтов
                    stream.Write(Encoding.UTF8.GetBytes(xlen), 0, 8);

                    // полученное сервером - положить в ПОТОКОБЕЗОПАСНУЮ очередь для записи на диск
                    TCPServerQ.Enqueue(receivedData.ToString());
                    
                    // Закрываем потоки
                    stream.Close();
                    client.Close();
/*

                    //-----------------------------------------------------
                    Console.WriteLine("Ожидание подключений... ");

                    // получаем входящее подключение
                    TcpClient client = server.AcceptTcpClient();

                    Console.WriteLine("Подключен клиент. Выполнение запроса...");

                    // получаем сетевой поток для чтения и записи
                    NetworkStream stream = client.GetStream();

                    // сообщение для отправки клиенту
                    string response = "Привет мир";
                    // преобразуем сообщение в массив байтов
                    byte[] data = Encoding.UTF8.GetBytes(response);

                    // отправка сообщения
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine("Отправлено сообщение: {0}", response);
                    // закрываем поток
                    stream.Close();
                    // закрываем подключение
                    client.Close();
                    //----------------------------------------------
*/
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (server != null)
                    server.Stop();
            }
        }
    }

    class ClientTcp
    {
        #region Construction


        private int portX = 8888;
        private string serverX = "127.0.0.1";

        public ClientTcp(string addr, int port, string message, ManualResetEvent sendPositionOnStart, ManualResetEvent sendPositionOnFinish, ManualResetEvent sendResult)
        {
            sendPositionOnStart.Set();
            sendPositionOnFinish.Reset();
            sendResult.Reset();
            try
            {
                this.serverX = addr;
                this.portX = port;
                TcpClient client = new TcpClient();
                client.Connect(serverX, portX);

                NetworkStream stream = client.GetStream();
                string xlen = message.Length.ToString("X8");
                // преобразуем сообщение в массив байтов
                byte[] data = Encoding.UTF8.GetBytes(xlen + message);

                // отправка сообщения
                stream.Write(data, 0, data.Length);

                // назад должны приййти проверочные 8 байт как в заголовке
                byte[] backdata = new byte[8];
                StringBuilder receivedData = new StringBuilder();
                int readbytes = 0;
                //do
                //{
                readbytes = stream.Read(backdata, 0, backdata.Length);
                string response = Encoding.UTF8.GetString(backdata, 0, 8);
                if (xlen == response)
                {
                    // данные отправились
                    sendResult.Set();
                }

                //}
                //while (stream.DataAvailable); // пока данные есть в потоке

                // закрываем поток
                stream.Close();
                // закрываем подключение
                client.Close();
                // можно повторять отправку
                sendPositionOnStart.Reset();
                sendPositionOnFinish.Set();
                


                ////----------------------------------------
                //this.serverX = addr;
                //this.portX = port;
                //TcpClient client = new TcpClient();
                //client.Connect(serverX, portX);

                //byte[] data = new byte[256];
                //StringBuilder response = new StringBuilder();
                //NetworkStream stream = client.GetStream();

                //do
                //{
                //    int bytes = stream.Read(data, 0, data.Length);
                //    response.Append(Encoding.UTF8.GetString(data, 0, bytes));
                //}
                //while (stream.DataAvailable); // пока данные есть в потоке

                //Console.WriteLine(response.ToString());

                //// Закрываем потоки
                //stream.Close();
                //client.Close();
                ////----------------------------------------------
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }

            Console.WriteLine("Запрос завершен...");
            Console.Read();
        }
        #endregion
    }
    
    class TCPChannel
    {
        public TCPChannel(Queue SendingQ, Queue ReceivingQ)
        {
            // 2 анонимных потока: серверный и черпальщик из "очереди в сеть", с созданием клиента для отправки
        }
    } 
}