using System.Net.Sockets;
using System.Net;
using TCPClientExtensions;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Text;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Numerics;

namespace AsyncChat
{
    internal class Program
    {
        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";
        private static Random random = new Random();

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
                ClientEntity clientEntity = await AddNewClientAsync(listener);

                Task.Run(() => ProcessClientAsync(clientEntity));
            }
        }

        private static async Task<ClientEntity> AddNewClientAsync(TcpListener listener)
        {
            TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);

            string login = await HandleNewClientLoginAsync(client);

            Console.WriteLine($"Adding client {login}");

            ClientEntity newClient = new ClientEntity(client, login);
            newClient.IsLoggedIn = true;
            _clients.TryAdd(login, newClient);

            return newClient;
        }

        private static async Task<string> HandleNewClientLoginAsync(TcpClient client)
        {
            bool isLoginSucceeded = false;

            string login = await AskForLoginAsync(client, "Please, input your login.");

            while (!isLoginSucceeded)
            {
                if (login == "all")
                {
                    login = await AskForLoginAsync(client, "'All' word is reserved and can't be used as login. Please, input new login.");
                }
                else if (_clients.ContainsKey(login) && _clients[login].IsLoggedIn)
                {
                    login = await AskForLoginAsync(client, $"User with {login} is already logged in. Please, input new login.");
                }
                else
                {
                    isLoginSucceeded = true;

                    ServerLoginResponce succesLoginResponce = new ServerLoginResponce($"You were successfully logged in as {login}.", true, login);

                    await client.WriteCustomAsync(JsonSerializer.Serialize(succesLoginResponce), false);

                    break;
                }
            }

            return login;
        }

        public static async Task<string> AskForLoginAsync(TcpClient client, string message)
        {
            ServerLoginResponce loginQuery = new ServerLoginResponce(message, false);

            await client.WriteCustomAsync(JsonSerializer.Serialize(loginQuery), false);

            string loginResponceMessage = await client.ReadCustomAsync(false);
            ClientLoginResponce loginResponce = JsonSerializer.Deserialize<ClientLoginResponce>(loginResponceMessage);
            string login = loginResponce.Login;

            return login;
        }

        public static async Task ProcessClientAsync(ClientEntity clientEntity)
        {
            if (clientEntity.IsLoggedIn)
            {
                while (true)
                {
                    string message = await clientEntity.Client.ReadCustomAsync(false);

                    if (message != null && message.Length != 0)
                    {
                        
                        bool isGetNewMessagesRequest = await HandleGetNewMessagesRequest(message, clientEntity);
                        if (isGetNewMessagesRequest)
                            continue;

                        bool isSendMessageRequest = await HandleSendMessageRequest(message, clientEntity);
                        if (isGetNewMessagesRequest)
                            continue;
                        
                        await HandleClientDefaultResponce(message, clientEntity);
                    }
                }
            }
        }

        private static async Task HandleClientDefaultResponce(string message, ClientEntity clientEntity)
        {
            Console.WriteLine($"Client {clientEntity.Login} message : {message}");
        }

        private static async Task<bool> HandleSendMessageRequest(string message, ClientEntity clientEntity)
        {
            bool isSendMessageRequest = false;

            try
            {
                ClientMessageData clientMessageData = JsonSerializer.Deserialize<ClientMessageData>(message);

                isSendMessageRequest = true;
                try
                {
                    TcpClient receiverClient = _clients.First(c => c.Key.Trim().Equals(clientMessageData.LoginFrom)).Value.Client;

                    Console.WriteLine($"Sending message {message} from client {clientMessageData.LoginFrom} to client {clientMessageData.LoginTo}");

                    await receiverClient.WriteCustomAsync($"{clientMessageData.TimeSent} {clientMessageData.LoginFrom} : {message}", false);
                }
                catch
                {
                    await clientEntity.Client.WriteCustomAsync($"Cant send message to client with login {clientMessageData.LoginTo}, because it wasnt found.", false);
                }
            }
            catch
            {

            }

            return isSendMessageRequest;
        }


        private static async Task<bool> HandleGetNewMessagesRequest(string message, ClientEntity clientEntity)
        {
            bool isGetNewMessageRequest = false;

            try
            {
                AskForNewMessagesRequest askForNewMessagesRequest = JsonSerializer.Deserialize<AskForNewMessagesRequest>(message);

                isGetNewMessageRequest = true;

                Console.WriteLine($"Received get new messages request from client {clientEntity.Login}");
            }
            catch
            {

            }

            return isGetNewMessageRequest;
        }

        public static async Task<string> GenerateRandomMessage()
        {
            await Task.Delay(random.Next(2000, 6000));
            return ExtendedTCPClient.GenerateRandomLatinString(10);
        }

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
            //заблокировать потоки клиентов для записи, пока туда пишет сервер?
            //Task.WaitAll(broadcastToClientTasks.ToArray());
        }

        public class ClientEntity
        {

            public string HostName { get; init; }
            public int Port { get; init; }
            public TcpClient Client { get; init; }
            public string Login { get; init; }
            public bool IsLoggedIn { get; set; } = false;

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
