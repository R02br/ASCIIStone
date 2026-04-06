using System.Net;
using System.Net.Sockets;

public class Connection
{
    public uint id;
    public Socket TcpSocket;
    public IPEndPoint UdpEndpoint;

    public Connection(uint id, Socket tcpSocket, IPEndPoint udpEndpoint)
    {
        this.id = id;
        this.TcpSocket = tcpSocket;
        this.UdpEndpoint = udpEndpoint;
    }
}