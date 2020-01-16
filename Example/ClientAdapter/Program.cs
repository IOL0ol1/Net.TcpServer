using Net.TcpServer;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientAdapter
{
    class Program
    {
        /// <summary>
        /// a echo client adapter
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string msgToSend = "echo";
            IPAddress serverAddr = TcpServer.GetLocalAddress()[0];
            int serverPort = 65100;
            TcpClient tcpClient = new TcpClient(serverAddr.ToString(), serverPort);
            TcpConnection tcpConnection = new TcpConnection(tcpClient, _ =>
            {
                // accept is connect in client adapter 
                // send message when client connected.
                _.OnAccept = client =>
                {
                    Console.WriteLine($"Client {tcpClient.Client.LocalEndPoint} is connected!");
                    client.Send(msgToSend, endPoint => Console.WriteLine($"Client {endPoint} send complated"));
                };
                _.OnReceive = (client, data) =>
                {
                    Console.WriteLine($"OnReceive(Hex): {BitConverter.ToString(data)}");
                    client.Send(data, endPoint => Console.WriteLine($"Client {endPoint} send complated"));
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
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            tcpConnection.Dispose();
        }
    }
}
