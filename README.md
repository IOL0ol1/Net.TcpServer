# Net.TcpServer
A single-file event asynchronous(APM) tcp server and tcp client adapter for tcp debug assistant

[![Nuget](https://img.shields.io/nuget/v/Net.TcpServer)](https://www.nuget.org/packages/Net.TcpServer/) [![Nuget](https://img.shields.io/nuget/dt/Net.TcpServer)](https://www.nuget.org/packages/Net.TcpServer/)
[![Build status](https://ci.appveyor.com/api/projects/status/a47ofnmvbqt65hg9?svg=true)](https://ci.appveyor.com/project/IOL0ol1/net-tcpserver)


# Quick start
```ps
Install-Package Net.TcpServer -Version 1.0.0
```
```csharp
using Net.TcpServer;
```
```csharp
// A echo server
TcpServer tcpServer = new TcpServer(IPAddress.Any, TcpServer.GetFreePort());
tcpServer.Start(_ =>
{
    _.OnAccept = client =>
    {
        Console.WriteLine($"OnAccept: {client}");
    };
    _.OnReceive = (client, data) =>
    {
        Console.WriteLine($"OnReceive: {client} {Encoding.UTF8.GetString(data)}");
        client.Send(data, endPoint => Console.WriteLine($"Send: {endPoint} complated"));
    };
    _.OnError = (client, ex) =>
    {
        Console.WriteLine($"OnError: {client} {ex.Message}");
    };
    _.OnClose = (client, isCloseByClient) =>
    {
        Console.WriteLine($"OnClose: {client} {(isCloseByClient ? "by client" : "by server")}");
    };
});
Console.ReadKey();
tcpServer.Stop();
```

## Other
a client adapter see [Example/ClientAdapter](/Example/ClientAdapter)    
a multi-client server see [Example/ServerExample](/Example/ServerExample)