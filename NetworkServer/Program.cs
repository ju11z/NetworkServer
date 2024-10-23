using System.Net.Sockets;
using System.Net;
using TCPClientExtensions;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NetworkServer
{
    public class Program
    {
        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";

        static void Main(string[] args)
        {
            Task2_DoAdvancedServerWork();
            //Task1_DoSimpleServerWork();
        }

        public static void Task2_DoAdvancedServerWork()
        {
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");
            listener.Start();

            object locker = new object();

            using (TcpClient client = listener.AcceptTcpClient())
            {
                int promisedQueriesAmount = 10;

                while (true)
                {
                    CustomResponce customResponce = new CustomResponce();
                    try
                    {
                        //если убрать - Unexpected error occured when trying to read message from TCPClient: Array dimensions exceeded supported range.
                        lock (locker)
                        {
                            string jsonCustomQuery = client.ReadCustom();
                            Console.WriteLine(jsonCustomQuery);

                            try
                            {
                                CustomQuery cq = JsonSerializer.Deserialize<CustomQuery>(jsonCustomQuery);
                                customResponce.QueryNumber = cq.QueryNumber;

                                if (cq.RandomInt % 15 == 0)
                                {
                                    customResponce.Responce = "foobar";
                                }
                                else if (cq.RandomInt % 5 == 0)
                                {
                                    customResponce.Responce = "foo";

                                }
                                else if (cq.RandomInt % 3 == 0)
                                {
                                    customResponce.Responce = "bar";
                                }

                                client.WriteCustom(customResponce.ToJson());
                            }
                            catch (Exception ex)
                            {
                                customResponce.ResponseCode = ResponceCode.ClientError;
                                customResponce.Responce = $"Error occured when deserialize message {jsonCustomQuery}: {ex.Message}";
                                Console.WriteLine(customResponce.Responce);
                                client.WriteCustom(customResponce.ToJson());
                            }
                            
                        }
                    }
                    catch (Exception e)
                    {
                        customResponce.ResponseCode = ResponceCode.ServerError;
                        customResponce.Responce = $"Unexpected error occured when trying to read message from TCPClient: {e.Message}";
                        Console.WriteLine(customResponce.Responce);
                    }
                }
                    
            }

            Console.ReadLine();

            listener.Stop();
        }


        public static void Task1_DoSimpleServerWork()
        {
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");
            listener.Start();

            TcpClient client = listener.AcceptTcpClient();

            string receivedMessage = client.ReadCustom();

            if (receivedMessage == "Hello")
            {
                client.WriteCustom("World");
            }
            else
            {
                client.WriteCustom("Error");
            }

            client.Close();
            listener.Stop();
        }
    }
}