using System.Net.Sockets;
using System.Net;
using TCPClientExtensions;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Text;
using System.Collections.Generic;

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

        //if broadcast message from cl1 received -
        //wait when read/write operations on cl1 and cl2 streams will end.
        //queue operations
        //lock cl2 and cl3 streams
        //write to their streams broascast messages
        //unlock

        public static async Task Task3_DoAsyncChatServerWork()
        {
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");

            listener.Start();

            int iterationCounter = 0;

            while (true)
            {
                ClientEntity clientEntity = await AddNewClientAsync(listener);

                Task.Run(() => ProcessClientAsync(clientEntity));
            }
        }

        private static async Task<ClientEntity> AddNewClientAsync(TcpListener listener)
        {
            TcpClient newClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            string login = ExtendedTCPClient.GenerateRandomLatinString(3);

            Console.WriteLine($"Adding client {login}");

            ClientEntity clientEntity = new ClientEntity(newClient, login);
            _clients.TryAdd(login, clientEntity);

            return clientEntity;
        }

        public static async Task ProcessClientAsync(ClientEntity clientEntity)
        {
            while (true)
            {
                string message = await GetClientInputAsync(clientEntity);

                if(message.Length!=0 && message != null)
                {
                    BroadcastMessageAsync(clientEntity, message);
                }
            }
        }

        public static async Task<string> GetClientInputAsync(ClientEntity clientEntity)
        {
            string message = await clientEntity.Client.ReadCustomAsync(false);

            if (message.Length != 0 && message != null)
            {
                Console.WriteLine($"Received message {message} from client {clientEntity.Login}");
            }
            
            return message;
        }

        /*
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
        */
        public static void BroadcastMessageAsync(ClientEntity sender, string message)
        {
            List<Task> broadcastToClientTasks = new List<Task>();
            foreach (var client in _clients)
            {
                Console.WriteLine($"Sending broadcast message {message} to client {client.Value.Login}");
                //если добавлять таску в список и потом с помощью WaitAll всех их выполнять, то клиенты виснут: не получают сообщения и не высылают их
                //broadcastToClientTasks.Add(new Task(() => client.Value.Client.WriteCustomAsync(message)));
                Task.Run(() => client.Value.Client.WriteCustomAsync(message, false));
            }
            //заблокировать потоки клиентов для записи, пока туда пишет сервер
            //Task.WaitAll(broadcastToClientTasks.ToArray());
        }

        public class ClientEntity
        {

            public string HostName { get; init; }
            public int Port { get; init; }
            public TcpClient Client { get; init; }
            public string Login { get; init; }

            public ClientEntity(TcpClient client, string login)
            {
                Client = client;
                Login = login;

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
