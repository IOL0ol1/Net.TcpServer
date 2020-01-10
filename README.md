# Net.TcpServer
A single-file event asynchronous(APM) tcp server for tcp debug assistant


# example
```csharp
// a echo server
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
```
