using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

static class NetworkManager
{
    public static Dictionary<uint, Connection> ConnectionByID = new Dictionary<uint, Connection>();
    public static Dictionary<Socket, Connection> ConnectionByTcpSocket = new Dictionary<Socket, Connection>();
    public static Dictionary<IPEndPoint, Connection> ConnectionByUdpEndpoint = new Dictionary<IPEndPoint, Connection>();

    public static Socket? TcpSocket;
    public static Socket? UdpSocket;
    public static IPEndPoint? serverUdpEndpoint;
    public static Connection? serverConnection;

    public static bool isConnecting = false;
    public static bool isHost = false;

    public static void Cleanup()
    {
        try
        {
            TcpSocket?.Shutdown(SocketShutdown.Both);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        TcpSocket?.Close();
        UdpSocket?.Close();

        serverUdpEndpoint = null;
        serverConnection = null;

        isConnecting = false;

        isHost = false;

        ConnectionByID.Clear();
        ConnectionByTcpSocket.Clear();
        ConnectionByUdpEndpoint.Clear();
    }

    public static void HostGame()
    {
        UdpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
        UdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

        HandleIncomingUdp();

        TcpSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        TcpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
        TcpSocket.Listen();

        isHost = true;

        AcceptNewConnections();
    }

    public static async void AcceptNewConnections()
    {
        try
        {
            if (TcpSocket == null) return;

            while (true)
            {
                Socket client = await TcpSocket.AcceptAsync();

                if (Game.gameState == Game.GameStates.HostMenu) // can't accept yet
                {
                    DisconnectTcpClient(client);
                }
                else
                {
                    ProcessNewConnetion(client);
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public static async void ProcessNewConnetion(Socket client)
    {
        try
        {
            //untrusted connection yet - small buffer size
            byte[] buffer = new byte[64];

            int totalBytesReceived = 0;
            int lengthPrefixSize = sizeof(int);

            while (totalBytesReceived < lengthPrefixSize)
            {
                int bytesRead = await client.ReceiveAsync(buffer.AsMemory(totalBytesReceived, lengthPrefixSize - totalBytesReceived));

                if (bytesRead == 0)
                {
                    DisconnectTcpClient(client);
                    return;
                }
                else
                {
                    totalBytesReceived += bytesRead;
                }
            }

            totalBytesReceived = 0;
            int bytesToReceive = BitConverter.ToInt32(buffer, 0);

            if (isHost)
            {
                if (bytesToReceive > HandshakePacket.worstPacketSize)
                {
                    DisconnectTcpClient(client);
                    return;
                }
            }
            else
            {
                if (bytesToReceive != ServerInfo.packetSize)
                {
                    DisconnectTcpClient(client);
                    return;
                }
            }

            while (totalBytesReceived < bytesToReceive)
            {
                int bytesRead = await client.ReceiveAsync(buffer.AsMemory(totalBytesReceived, bytesToReceive - totalBytesReceived));

                if (bytesRead == 0)
                {
                    DisconnectTcpClient(client);
                    return;
                }
                else
                {
                    totalBytesReceived += bytesRead;
                }
            }

            Packet.ParsePacket(new ParseInput(buffer, new Connection(0, client, new IPEndPoint(IPAddress.Any, 0)), true));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public static async void HandleIncomingUdp()
    {
        if (UdpSocket == null) return;

        byte[] buffer = new byte[1024];
        IPEndPoint udpEndpoint = new IPEndPoint(IPAddress.Any, 0);
        Connection? conn;

        while (!(Game.gameState == Game.GameStates.MainMenu))
        {
            try
            {
                SocketReceiveFromResult result = await UdpSocket.ReceiveFromAsync(buffer, udpEndpoint);

                if (isHost)
                {
                    if (ConnectionByUdpEndpoint.TryGetValue((IPEndPoint)result.RemoteEndPoint, out conn))
                    {
                        if (conn != null)
                        {
                            Packet.ParsePacket(new ParseInput((buffer.AsMemory(0, result.ReceivedBytes)).ToArray(), conn, false));
                        }
                    }
                }
                else
                {
                    if (serverConnection != null)
                    {
                        IPEndPoint outputIPEndPoint = (IPEndPoint)result.RemoteEndPoint;

                        serverUdpEndpoint = outputIPEndPoint;

                        if (outputIPEndPoint.Address.MapToIPv6().GetAddressBytes().SequenceEqual(serverConnection.UdpEndpoint.Address.MapToIPv6().GetAddressBytes()))
                        {
                            if (outputIPEndPoint.Port == serverConnection.UdpEndpoint.Port)
                            {
                                Packet.ParsePacket(new ParseInput((buffer.AsMemory(0, result.ReceivedBytes)).ToArray(), serverConnection, false));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }

    public static async void HandleConnection(Connection conn)
    {
        byte[] buffer = new byte[1024];

        int lengthPrefixSize = sizeof(int);
        int bytesToReceive;
        int totalBytesReceived;

        bool isConnected = true;

        try
        {
            while (isConnected)
            {
                totalBytesReceived = 0;

                //get length prefix
                while (totalBytesReceived < lengthPrefixSize)
                {
                    int bytesRead = await conn.TcpSocket.ReceiveAsync(buffer.AsMemory(totalBytesReceived, lengthPrefixSize - totalBytesReceived));

                    if (bytesRead <= 0)
                    {
                        isConnected = false;
                        break;
                    }

                    totalBytesReceived += bytesRead;
                }

                totalBytesReceived = 0;
                bytesToReceive = BitConverter.ToInt32(buffer, 0);

                if (bytesToReceive <= 0 || bytesToReceive > Packet.maxPacketSize)
                {
                    Debug.WriteLine($"Packet max size exceeded. Current max size: {Packet.maxPacketSize}. Tried to receive {buffer.Length}");

                    if (isHost)
                    {
                        RemoveConnection(conn);
                    }

                    return;
                }

                //get actuall packet
                while (totalBytesReceived < bytesToReceive)
                {
                    int bytesRead = await conn.TcpSocket.ReceiveAsync(buffer.AsMemory(totalBytesReceived, bytesToReceive - totalBytesReceived));

                    if (bytesRead <= 0)
                    {
                        isConnected = false;
                        break;
                    }

                    totalBytesReceived += bytesRead;
                }

                Packet.ParsePacket(new ParseInput(buffer, conn, true));
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        if (isHost)
        {
            RemoveConnection(conn);
            return;
        }
        else
        {
            Game.QuitToMenu();
        }
    }

    public static async void ConnectToServer(IPEndPoint endPoint)
    {
        try
        {
            UdpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            UdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

            if (UdpSocket == null || UdpSocket.LocalEndPoint == null)
            {
                Game.QuitToMenu();
                return;
            }

            HandleIncomingUdp();

            TcpSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            TcpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

            isConnecting = true;
            await TcpSocket.ConnectAsync(endPoint);

            SendPacket(HandshakePacket.Build(((IPEndPoint)(UdpSocket.LocalEndPoint)).Port, Player.myPlayerChar), TcpSocket);
            ProcessNewConnetion(TcpSocket);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        isConnecting = false;
    }

    public static void DisconnectTcpClient(Socket client)
    {
        try
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public static void AddConnection(Connection conn)
    {
        ConnectionByID.Add(conn.id, conn);
        ConnectionByTcpSocket.Add(conn.TcpSocket, conn);
        ConnectionByUdpEndpoint.Add(conn.UdpEndpoint, conn);
    }

    public static void RemoveConnection(Connection conn)
    {
        DisconnectTcpClient(conn.TcpSocket);

        Entity.DestroyEntity(conn.id, true);

        ConnectionByID.Remove(conn.id);
        ConnectionByTcpSocket.Remove(conn.TcpSocket);
        ConnectionByUdpEndpoint.Remove(conn.UdpEndpoint);
    }

    public static async void SendPacket(byte[] buffer, Socket? tcpSocket)
    {
        if (tcpSocket == null) return;

        if (buffer.Length > Packet.maxPacketSize)
        {
            throw new Exception($"Packet max size exceeded. Current max size: {Packet.maxPacketSize}. Tried to send {buffer.Length}");
        }

        try
        {
            await tcpSocket.SendAsync(BitConverter.GetBytes(buffer.Length));
            await tcpSocket.SendAsync(buffer);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public static async void SendPacket(byte[] buffer, IPEndPoint receiver)
    {
        if (UdpSocket == null)
        {
            throw new Exception("UdpSocket uninitialized");
        }

        await UdpSocket.SendToAsync(buffer, receiver);
    }

    public static void SendToAllClients(byte[] buffer, bool useTcp, Connection? connectionToSkip)
    {
        if (!isHost) return;

        foreach (Connection conn in ConnectionByID.Values)
        {
            if ((connectionToSkip == null) || (connectionToSkip != conn))
            {
                if (useTcp)
                {
                    SendPacket(buffer, conn.TcpSocket);
                }
                else
                {
                    SendPacket(buffer, conn.UdpEndpoint);
                }
            }
        }
    }
}