using Net.TcpServer;

using System;
using System.Net;
using System.Text;

namespace EchoServer
{
    class Program
    {
        /// <summary>
        /// a echo server
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            TcpServer tcpServer = new TcpServer(IPAddress.Any, TcpServer.GetFreePort());
            tcpServer.Start(_ =>
            {
                _.OnAccept = client =>
                {
                    Console.WriteLine($"OnAccept: {client}");
                };
                _.OnReceive = (client, data) =>
                {
                    Console.WriteLine($"OnReceive(Hex): {client} {BitConverter.ToString(data)}");
                    client.Send(data, endPoint => Console.WriteLine($"Send: {endPoint} complated"));
                };
                _.OnError = (client, ex) =>
                {
                    Console.WriteLine($"OnError: {client} {ex.Message}");
                };
                _.OnClose = (client, isCloseByClient) =>
                {
                    Console.WriteLine($"OnClose: {client} close {(isCloseByClient ? "by client" : "by server")}");
                };
            });
            Console.WriteLine($"Echo server {tcpServer} is start!");
            Console.WriteLine($"Press any key to exit");
            Console.ReadKey();
            tcpServer.Stop();
        }
    }
}
