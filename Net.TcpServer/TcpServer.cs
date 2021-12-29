using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Net.TcpServer
{
    /// <summary>
    /// Event-style tcp listener.
    /// </summary>
    public class TcpServer
    {
        private bool isClosing = false;
        private TcpListener server;
        private Action<TcpConnection> connectinAction; // delegate for each client connection

        /// <summary>
        /// Server status.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpServer"/> class with the
        /// specified local endpoint.
        /// </summary>
        /// <param name="localEndPoint">An <see cref="IPEndPoint"/> that represents the local endpoint to which to bind
        /// the listener.</param>
        /// <exception cref="ArgumentNullException">localEP is null.</exception>
        public TcpServer(IPEndPoint localEndPoint)
        {
            IsRunning = false;
            server = new TcpListener(localEndPoint);
        }

        /// <summary>
        /// Initializes a new instance of the<see cref="TcpServer"/> class that listens
        /// for incoming connection attempts on the specified local IP address and port number.
        /// </summary>
        /// <param name="address">An <see cref="IPAddress"/> that represents the local IP address.</param>
        /// <param name="port">The port on which to listen for incoming connection attempts.</param>
        /// <exception cref="ArgumentNullException">localaddr is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">port is not between <see cref="IPEndPoint.MinPort"/> 
        /// and <see cref="IPEndPoint.MaxPort"/>.</exception>
        public TcpServer(IPAddress address, int port) : this(new IPEndPoint(address, port))
        {
        }

        /// <summary>
        /// Initializes a new instance of the<see cref="TcpServer"/> class that listens
        /// for incoming connection attempts on the specified local IP address and port number.
        /// </summary>
        /// <param name="address">An <see cref="IPAddress"/> that represents the local IP address.</param>
        /// <param name="port">The port on which to listen for incoming connection attempts.</param>
        /// <exception cref="ArgumentNullException">address is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">port is not between <see cref="IPEndPoint.MinPort"/> 
        /// and <see cref="IPEndPoint.MaxPort"/>.</exception>
        /// <exception cref="FormatException">address is not a valid IP address.</exception>
        public TcpServer(string address, int port) : this(IPAddress.Parse(address), port)
        {
        }

        /// <summary>
        /// Starts listening for incoming connection requests.
        /// </summary>
        /// <param name="action">delegate for each client connection</param>
        /// <exception cref="SocketException">
        /// Use the <see cref="SocketException.ErrorCode"/> property to obtain the specific
        /// error code. When you have obtained this code, you can refer to the Windows Sockets
        /// version 2 API error code documentation in MSDN for a detailed description of
        /// the error.</exception>
        public void Start(Action<TcpConnection> action)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                isClosing = false;
                connectinAction = action;
                server.Start();
                StartAccept();
            }
        }

        /// <summary>
        ///  Gets the underlying <see cref="EndPoint"/> of the current <see cref="TcpServer"/>.
        /// </summary>
        public EndPoint LocalEndPoint
        {
            get { return server.LocalEndpoint; }
        }

        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="TcpServer"/>
        /// allows only one underlying socket to listen to a specific port.
        /// </summary>
        public bool ExclusiveAddressUse
        {
            get { return server.ExclusiveAddressUse; }
            set { server.ExclusiveAddressUse = value; }
        }

        /// <summary>
        ///  Gets the underlying network <see cref="Socket"/>.
        /// </summary>
        public Socket Server
        {
            get { return server.Server; }
        }

        /// <summary>
        /// Closes the listener.
        /// </summary>
        public void Stop()
        {
            isClosing = true;
            server.Stop();
            IsRunning = false;
        }

        /// <summary>
        /// Begins an asynchronous operation to accept an incoming connection attempt.
        /// </summary>
        private void StartAccept()
        {
            server.BeginAcceptTcpClient(iar =>
            {
                try
                {
                    if (isClosing) return;
                    TcpClient tcpClient = server.EndAcceptTcpClient(iar);
                    TcpConnection client = new TcpConnection(tcpClient, connectinAction);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                }
                StartAccept();
            }, null);

        }

        /// <summary>
        /// Returns the IP address and port number of the specified endpoint.
        /// </summary>
        /// <returns>A string containing the IP address and the port number of the specified endpoint
        /// (for example, 192.168.1.2:80).</returns>
        public override string ToString()
        {
            return LocalEndPoint.ToString();
        }

        /// <summary>
        /// Get local IP addresses.
        /// </summary>
        /// <param name="addressFamily">Address family.</param>
        /// <returns>A array of local IP addresses.</returns>
        public static IPAddress[] GetLocalAddress(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            List<IPAddress> list = new List<IPAddress>();
            foreach (var item in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (item.AddressFamily == addressFamily)
                    list.Add(item);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Get active tcp IPEndPoint.
        /// </summary>
        /// <returns>A array of active tcp IPEndPoint.</returns>
        public static IEnumerable<IPEndPoint> GetActiveTcpPorts()
        {
            List<IPEndPoint> localEndPoints = new List<IPEndPoint>();
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            localEndPoints.AddRange(ipGlobalProperties.GetActiveTcpListeners());
            localEndPoints.AddRange(ipGlobalProperties.GetActiveTcpConnections().Select(item => item.LocalEndPoint));
            return localEndPoints.Distinct();
        }

        /// <summary>
        /// Randomly get an idle port greater than min
        /// </summary>
        /// <param name="min">Specify the minimum port.</param>
        /// <param name="address">Specify IP address, null is all.</param>
        /// <returns>A array of active tcp ports that specify the address.</returns>
        public static int GetFreePort(int min = 1024, IPAddress address = null)
        {
            int freePort = -1;
            Random random = new Random();
            int[] freePorts = GetActiveTcpPorts()
                .Where(_ => _.Address.Equals(address) || address == null)
                .Select(_ => _.Port)
                .Where(x => x >= (min = min <= 0 ? 1 : min))
                .ToArray();
            while (freePort < 0)
            {
                freePort = random.Next(min + 1, 65536);
                foreach (var item in freePorts)
                {
                    if (freePort == item)
                        freePort = -1;
                }
            }
            return freePort;
        }
    }

    /// <summary>
    ///  Provides client connections for TCP network services.
    /// </summary>
    public class TcpConnection : IDisposable
    {
        private bool isClosing = false;
        private byte[] receiveBuffer;
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        // private readonly Action<TcpConnection> initialize;

        /// <summary>
        /// <see cref="TcpClient"/> adapter.
        /// </summary>
        /// <param name="client">The specified client.</param>
        /// <param name="action">client action.</param>
        public TcpConnection(TcpClient client, Action<TcpConnection> action)
        {
            tcpClient = client;
            //initialize = action;
            networkStream = tcpClient.GetStream();
            RemoteEndPoint = CopyEndPoint(tcpClient.Client.RemoteEndPoint);
            action?.Invoke(this);
            OnAccept?.Invoke(this);
            receiveBuffer = new byte[tcpClient.ReceiveBufferSize];
            StartReceive();
        }

        /// <summary>
        /// Gets the <see cref="EndPoint"/> deep copy.
        /// </summary>
        /// <param name="endPoint">source</param>
        /// <returns>deep copy object</returns>
        private static EndPoint CopyEndPoint(EndPoint endPoint)
        {
            return endPoint.Create(endPoint.Serialize());
        }

        /// <summary>
        /// Begins an asynchronous read from the <see cref="NetworkStream"/>.
        /// </summary>
        private void StartReceive()
        {
            try
            {
                networkStream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, iar =>
                {
                    try
                    {
                        if (isClosing) return;
                        int size = networkStream.EndRead(iar);
                        if (size == 0 || !tcpClient.Connected || !networkStream.CanRead)
                        {
                            Close(true);
                            return;
                        }
                        byte[] data = new byte[size];
                        Buffer.BlockCopy(receiveBuffer, 0, data, 0, size);
                        OnReceive?.Invoke(this, data);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(this, ex);
                    }
                    StartReceive();
                }, null);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Gets or sets a value that disables a delay when send or receive buffers are not
        /// full.
        /// </summary>
        public bool NoDelay
        {
            get { return tcpClient.NoDelay; }
            set { tcpClient.NoDelay = value; }
        }

        /// <summary>
        /// Gets the amount of data that has been received from the network and is available
        /// to be read.
        /// </summary>
        public int Available
        {
            get { return tcpClient.Available; }
        }

        /// <summary>
        /// Gets or sets the underlying <see cref="Socket"/>.
        /// </summary>
        public Socket Client
        {
            get { return tcpClient.Client; }
        }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndPoint RemoteEndPoint
        {
            get; private set;
        }

        /// <summary>
        /// Gets a value indicating whether the underlying <see cref="Socket"/> for
        /// a <see cref="TcpClient"/> is connected to a remote host.
        /// </summary>
        public bool Connected
        {
            get { return tcpClient.Connected; }
        }

        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="TcpClient"/>
        /// allows only one client to use a port.
        /// </summary>
        public bool ExclusiveAddressUse
        {
            get { return tcpClient.ExclusiveAddressUse; }
            set { tcpClient.ExclusiveAddressUse = value; }
        }

        /// <summary>
        /// Begins an asynchronous write to a stream.
        /// </summary>
        /// <param name="data">An array of type <see cref="byte"/> that contains the data to write to the <see cref="NetworkStream"/>.</param>
        /// <param name="action">send complated callback</param>
        /// <returns></returns>
        public IAsyncResult Send(byte[] data, Action<IPEndPoint> action = null)
        {
            try
            {
                return networkStream.BeginWrite(data, 0, data.Length, iar =>
                {
                    try
                    {
                        if (!tcpClient.Connected || !networkStream.CanRead)
                        {
                            Close(true);
                            return;
                        }
                        networkStream.EndWrite(iar);
                        action?.Invoke(RemoteEndPoint as IPEndPoint);
                    }
                    catch (Exception ex)
                    {
                        OnError(this, ex);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
            return null;
        }

        /// <summary>
        /// Begins an asynchronous write to a stream.
        /// </summary>
        /// <param name="message">An UTF8 <see cref="string"/> that contains the data to write to the <see cref="NetworkStream"/>.</param>
        /// <param name="action">send complated callback</param>
        /// <returns></returns>
        public IAsyncResult Send(string message, Action<IPEndPoint> action = null)
        {
            return Send(Encoding.UTF8.GetBytes(message), action);
        }

        /// <summary>
        /// Disposes this instance and requests that the underlying
        /// TCP connection be closed.
        /// </summary>
        /// <param name="isClosedByClient">is closed by client</param>
        private void Close(bool isClosedByClient)
        {
            isClosing = true;
            networkStream.Close();
            tcpClient.Close();
            OnClose?.Invoke(this, isClosedByClient);
        }

        /// <summary>
        /// Disposes this instance and requests that the underlying
        /// TCP connection be closed.
        /// </summary>
        public void Close()
        {
            Close(false);
        }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        /// <returns>A string containing the IP address and the port number of the specified endpoint
        /// (for example, 192.168.1.2:80).</returns>
        public override string ToString()
        {
            return RemoteEndPoint.ToString();
        }

        /// <summary>
        /// TcpConnection accept event
        /// </summary>
        public Action<TcpConnection> OnAccept { get; set; }

        /// <summary>
        /// TcpConnection receive event
        /// </summary>
        public Action<TcpConnection, byte[]> OnReceive { get; set; }

        /// <summary>
        /// TcpConnection close event
        /// </summary>
        public Action<TcpConnection, bool> OnClose { get; set; }

        /// <summary>
        /// TcpConnection error event
        /// </summary>
        public Action<TcpConnection, Exception> OnError { get; set; }

        #region IDisposable Support
        private bool disposedValue = false;

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close(false);
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
