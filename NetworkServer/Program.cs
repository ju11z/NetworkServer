using System.Net.Sockets;
using System.Net;
using TCPClientExtensions;
using System.Text.Json;

namespace NetworkServer
{
    public class Program
    {
        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";

        static void Main(string[] args)
        {
            DoAdvancedServerWork();
        }


        public static void DoAdvancedServerWork()
        {
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");
            listener.Start();

            using (TcpClient client = listener.AcceptTcpClient())
            {
                string receivedMessage = client.ReadCustom();
                int promisedQueriesAmount = Int32.Parse(receivedMessage);

                Parallel.For(0, promisedQueriesAmount, (i, state) =>
                {
                    CustomResponce customResponce = new CustomResponce();
                    try
                    {
                        string jsonCustomQuery = client.ReadCustom();

                        try
                        {
                            CustomQuery cq = JsonSerializer.Deserialize<CustomQuery>(jsonCustomQuery);

                            if (cq.RandomInt % 3 == 0)
                            {
                                customResponce.QueryNumber = cq.QueryNumber;
                                customResponce.Responce = "foo";
                                client.WriteCustom(customResponce.ToJson());
                            }
                            if (cq.RandomInt % 5 == 0)
                            {
                                customResponce.QueryNumber = cq.QueryNumber;
                                customResponce.Responce = "bar";
                                client.WriteCustom(customResponce.ToJson());
                            }
                            if (cq.RandomInt % 15 == 0)
                            {
                                customResponce.QueryNumber = cq.QueryNumber;
                                customResponce.Responce = "foobar";
                                client.WriteCustom(customResponce.ToJson());
                            }
                        }
                        catch (Exception ex)
                        {
                            customResponce.ResponseCode = ResponceCode.ClientError;
                            customResponce.Responce = $"Error occured when deserialize message {jsonCustomQuery}: {ex.Message}";
                            client.WriteCustom(customResponce.ToJson());
                        }
                    }
                    catch (Exception e)
                    {
                        customResponce.ResponseCode = ResponceCode.ServerError;
                        customResponce.Responce = $"Unexpected error occured when trying to read message from TCPClient: {e.Message}";
                        client.WriteCustom(customResponce.ToJson());
                    }
                });
            }

            listener.Stop();
        }

        public static void DoSimpleServerWork()
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