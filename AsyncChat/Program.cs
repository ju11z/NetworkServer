using System.Net.Sockets;
using System.Net;
using TCPClientExtensions;
using System.Net.Http;
using System.Collections.Concurrent;

namespace AsyncChat
{
    internal class Program
    {
        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";

        private static ConcurrentDictionary<string, ClientEntity> _clients = new ConcurrentDictionary<string, ClientEntity>();
        private static List<MessageData> _messageData = new List<MessageData>();

        static async Task Main(string[] args)
        {
            await Task3_DoAsyncChatServerWork();
        }

        public static async Task Task3_DoAsyncChatServerWork()
        {
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");

            listener.Start();

            int iterationCounter = 0;

            while (true)
            {
                await AddNewClientAsync(listener);

                foreach(var clientData in _clients)
                {
                    Task.Run(() => ProcessClientAsync(clientData.Value.Client));
                }
            }
        }

        private static async Task AddNewClientAsync(TcpListener listener)
        {
            TcpClient newClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

            _clients.TryAdd("aboba", new ClientEntity(newClient));
        }

        public static async Task ProcessClientAsync(TcpClient client)
        {
            string message =  client.ReadCustom();

            Task.Run(()=> BroadcastMessageAsync(message));
        }


        private static void HandleClient(TcpClient client)
        {
            client.WriteCustom("Please input your login", false);

            bool isUserLogedIn = false;

            while (!isUserLogedIn)
            {
                string login = client.ReadCustom(false);

                if (_clients.ContainsKey(login))
                {
                    client.WriteCustom("The name is already taken. Please input another one.", false);
                }
                else if (login == "all")
                {
                    client.WriteCustom("The word 'all' is reserved and cannot be used for login. Please input another one.", false);
                }
                else
                {
                    //_clients.Add(login, new ClientEntity(client));

                    isUserLogedIn = true;
                }
            }
        }

        protected internal static async Task BroadcastMessageAsync(string message)
        {
            foreach (var client in _clients)
            {
                 client.Value.Client.WriteCustom(message);
            }
        }

        public class ClientEntity
        {

            public string HostName { get; init; }
            public int Port { get; init; }
            public TcpClient Client { get; init; }

            public ClientEntity(TcpClient client)
            {
                Client = client;

                IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;

                IPAddress ipAddress = endPoint.Address;

                IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                HostName = hostEntry.HostName;

                Port = endPoint.Port;
            }

        }

        public class MessageData
        {
            public ClientEntity From { get; set; }
            public ClientEntity To { get; set; }
            public string Text { get; set; }
            public DateTime TimeSent { get; set; }
        }
    }

}
