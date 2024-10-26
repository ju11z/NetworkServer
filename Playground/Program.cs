
using System.Collections.Concurrent;

namespace Playground
{
    internal class Program
    {
        private static BlockingCollection<string> _clients = new BlockingCollection<string>() { "aboba", "mogus" };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Input new client name and press enter...");

            while (true)
            {
                Task.Run(() => AddClientAsync());

                foreach (var client in _clients)
                {
                    await ProcessClientAsync(client);
                }
            }
        }

        private static async Task AddClientAsync()
        {
            string newClient = Console.ReadLine();

            _clients.Add(newClient);
        }

        public static async Task ProcessClientAsync(string client)
        {
            Console.WriteLine($"Start processing client {client}...");
            await Task.Delay(1000);
            Console.WriteLine($"Finish processing client {client}...");
        }
    }
}
