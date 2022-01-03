using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Trojan;
using TrojanExceptions;

namespace SocketData
{
    static class ConnectionManager
    {
        public static List<ConnectionSetup> connections = new List<ConnectionSetup>();
        public static Queue<string> DataQueue = new Queue<string>();
        public static string[]? DataArray;

        public static void OutputDataQueue()
        {
            Console.WriteLine("\r##########################################################################################################################################");
            DataArray = DataQueue.ToArray();
            foreach(string Data in DataArray)
            {
                string message = "[+] Received_FromClient[{0}] :: Message_Size[{1} bytes]";
                Console.WriteLine("\n\n\t" + message, Data, UTF8Encoding.UTF8.GetByteCount(Data));
                Console.Write("\t" + new String('*', message.Length + Data.Length - 4));
                DataQueue.Dequeue();  
            }
            Console.WriteLine("\n\n##########################################################################################################################################");
        }
    }

    public struct ConnectionSetup
    {
        private readonly Socket _s_conn;
        private byte[] _buffer;

        public Socket getConn()
        {
            if (_s_conn == null) throw new ExceptionHandler("[-] Connection socket can not be null : " + nameof(_s_conn));
            return(_s_conn);
        }

        public byte[] getBuffer()
        {
            if (_buffer == null) throw new ExceptionHandler("[-] Buffer can not be null : " + nameof(_buffer));
            return(_buffer);
        }

        public void setBuffer(byte[] buffer)
        {
            if (buffer.Length != _buffer.Length) throw new ExceptionHandler("[-] Buffer is not the correct size [1024] : " + nameof(buffer));
            if (buffer == null) throw new ExceptionHandler("[-] Buffer can not be null : " + nameof(buffer));

            _buffer = buffer;
        }

        public EndPoint getRemoteEndPoint()
        {
            if (_s_conn.RemoteEndPoint == null) throw new ExceptionHandler("[-] RemoteEndPoint can not be null : " + nameof(_s_conn.RemoteEndPoint));
            return(_s_conn.RemoteEndPoint);
        }

        public ConnectionSetup(Socket s_conn, byte[] buffer)
        {
            _s_conn = s_conn;
            _buffer = buffer;
        }
    }

    public struct socketSetup
    {
        private string _UUID;

        public string getUUID()
        {
            if (_UUID == null) throw new ExceptionHandler("[-] UUID can not be null : " + nameof(_UUID));
            return(_UUID);
        }

        private IPEndPoint? _s_EndPoint = null;

        public void setEndPoint(IPEndPoint s_EndPoint)
        {
            if (s_EndPoint == null) throw new ExceptionHandler("[-] EndPoint can not be null : " + nameof(s_EndPoint));
            _s_EndPoint = s_EndPoint;
        }

        public IPEndPoint getEndPoint()
        {
            if (_s_EndPoint == null) throw new ExceptionHandler("[-] EndPoint not set : " + nameof(_s_EndPoint));
            return(_s_EndPoint);
        }

        public Socket? sock = null;

        public readonly ushort port;
        public readonly uint buffSize;

        public byte[] buffer;

        public socketSetup(string UUID, ushort port, uint buffSize)
        {
            _UUID = UUID;

            this.port = port;
            this.buffSize = buffSize;
            
            buffer = new byte[buffSize];
        }
    }

    public class SocketHandler
    {
        public static readonly ushort PORT = 8080;
        public static readonly uint BUFFSIZE = 1024;
        public static readonly ushort MAX_CONNECTIONS = 10;

        public static readonly int CONNECT_TIMEOUT = 30000;
        public static readonly int SEND_TIMEOUT = 20000;
        public static readonly int RECEIVE_TIMEOUT = 15000;

        public static readonly int EVENT_CHECK_INTERVAL = 3000;

        protected SocketOperations _SocketOperations = new SocketOperations();

        private byte[] buffer = new byte[BUFFSIZE];

        public SocketHandler()
        {

        }
    }
}
