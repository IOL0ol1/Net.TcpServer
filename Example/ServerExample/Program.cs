using Net.TcpServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace ServerExample
{
    class Program
    {
        /// <summary>
        /// server send string to all client.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // client list
            List<TcpConnection> clients = new List<TcpConnection>();
            // get first local address (some machines have multiple network cards),
            // or use IPAddress.Any to listener all address.
            IPAddress localAddr = TcpServer.GetLocalAddress()[0];
            int localPort = TcpServer.GetFreePort();
            TcpServer server = new TcpServer(localAddr, localPort);
            server.Start(connection =>
            {
                connection.OnAccept = client =>
                {
                    // disable nagle algorithm for small packets sent frequently.
                    client.NoDelay = true;
                    clients.Add(client);
                    Console.WriteLine($"accept {client}");
                };
                connection.OnReceive = (client, data) =>
                {
                    Console.WriteLine($"receive from {client} (Hex):{BitConverter.ToString(data)}");
                };
                connection.OnClose = (client, isClosedByClient) =>
                {
                    if (isClosedByClient)
                        clients.Remove(client);
                    Console.WriteLine($"client {client} close:by {(isClosedByClient ? "itself" : "server")}");
                };
                connection.OnError = (client, ex) =>
                {
                    Console.WriteLine($"client {client} error:{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                };
            });
            Console.WriteLine($"server {server} start");
            Console.WriteLine($"input 'EXIT' to exit");
            Console.WriteLine($"input some char and press ENTER send to all client:");

            string input = Console.ReadLine();
            while (input.ToLower() != "exit")
            {
                clients.ForEach(client => client.Send(input, _ => Console.WriteLine($"send to {_} complete")));
                input = Console.ReadLine();
            }

            server.Stop();
            clients.ForEach(client => client.Close());  // close all client.
            clients.Clear();
            Console.WriteLine("server {0} stop", server);
            Console.ReadKey();
        }
    }
}
