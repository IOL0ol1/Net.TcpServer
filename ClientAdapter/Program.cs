using Net.TcpServer;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientAdapter
{
    class Program
    {
        static void Main(string[] args)
        {
            // a echo client
            string message = "echo";
            TcpClient tcpClient = new TcpClient(TcpServer.GetLocalAddress()[0].ToString(), 65100);
            TcpConnection tcpConnection = new TcpConnection(tcpClient, _ =>
            {
                _.OnAccept = client =>
                {
                    Console.WriteLine($"Client {tcpClient.Client.LocalEndPoint} is start!");
                    client.Send(message, endPoint => Console.WriteLine($"Send complated"));
                };
                _.OnReceive = (client, data) =>
                {
                    Console.WriteLine($"OnReceive: {Encoding.UTF8.GetString(data)}");
                    client.Send(data, endPoint => Console.WriteLine($"Send complated"));
                };
                _.OnError = (client, ex) =>
                {
                    Console.WriteLine($"OnError: {ex.Message}");
                };
                _.OnClose = (client, isCloseByClient) =>
                {
                    Console.WriteLine($"OnClose: {(isCloseByClient ? "by client" : "by server")}");
                };
            });
            Console.ReadKey();
            tcpConnection.Dispose();
        }
    }
}
