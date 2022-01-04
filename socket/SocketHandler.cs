using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Trojan;
using Trojan.exceptions;
using SocketData.operations;

namespace SocketData
{
    public enum ServerStatus
    {
        RUNNING = 0,
        STOPPED = 1,
        IDLE = 2,
        WAITRESPONSE = 3,
        OUTPUTTING = 4,
    }

    public enum CommandName
    {
        GATHER,
    }

    static class CommandExtension
    {
        public static string ExecuteCommand(this CommandName command)
        {
            switch(command)
            {
                case CommandName.GATHER:
                    return("AHEHE");
                default:
                    return("AHEHE?");
            }
        }
    }

    public class ConnectionManager
    {
        public static List<ConnectionSetup> connections = new List<ConnectionSetup>();
        public static Queue<string> DataQueue = new Queue<string>();
        public static string[]? DataArray;

        public static void OutputDataQueue()
        {
            Console.WriteLine("\r##########################################################################################################################################");
            DataArray = DataQueue.ToArray();
            foreach (string Data in DataArray)
            {
                string message = "[+] Received_FromClient[{0}] :: Message_Size[{1} bytes]";
                Console.WriteLine("\n\n\t" + message, Data, Encoding.UTF8.GetByteCount(Data));
                Console.Write("\t" + new string('*', message.Length + Data.Length - 4));
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
            return _s_conn;
        }

        public byte[] getBuffer()
        {
            if (_buffer == null) throw new ExceptionHandler("[-] Buffer can not be null : " + nameof(_buffer));
            return _buffer;
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
            return _s_conn.RemoteEndPoint;
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
            return _UUID;
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
            return _s_EndPoint;
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
        public string TruncationCheckCommand(string command, int startIndex, int length)
        {
            command = command.Substring(startIndex, command.Length - startIndex);
            if (command.Length < length)
            {
                return command;
            }
            return command.Substring(0, length);
        }

        public string CheckForCommand(string input) { return(""); }
        public string RespondToCommand(Socket sock) { return(""); }
        
        public static ushort PORT;
        public static uint BUFFSIZE;

        public static ushort MAX_CONNECTIONS;

        public static int CONNECT_TIMEOUT;
        public static int SEND_TIMEOUT;
        public static int RECEIVE_TIMEOUT;
        public static int EVENT_CHECK_INTERVAL;

        protected SocketOperations _SocketOperations = new SocketOperations();
        public ServerStatus ServerStatus = ServerStatus.STOPPED;
        public ServerStatus SenderStatus = ServerStatus.IDLE;

        private byte[] buffer = new byte[BUFFSIZE];

        public SocketHandler()
        {
            using (StreamReader ConfigFile = new StreamReader(@"..\..\..\TrojanBuild\trojan.cfg"))
            {
                if (ConfigFile == null) throw new ExceptionHandler("Config file not found : " + nameof(ConfigFile));
                string line;
                while ((line = ConfigFile.ReadLine()) != null)
                {
                    string[] LineParts = line.Split(" ");
                    switch (LineParts[0])
                    {
                        case "port":
                            PORT = ushort.Parse(LineParts[2]);
                            break;
                        case "buffsize":
                            BUFFSIZE = uint.Parse(LineParts[2]);
                            break;
                        case "max_connections":
                            MAX_CONNECTIONS = ushort.Parse(LineParts[2]);
                            break;
                        case "connect_timeout":
                            CONNECT_TIMEOUT = int.Parse(LineParts[2]);
                            break;
                        case "send_timeout":
                            SEND_TIMEOUT = int.Parse(LineParts[2]);
                            break;
                        case "receive_timeout":
                            RECEIVE_TIMEOUT = int.Parse(LineParts[2]);
                            break;
                        case "event_check_interval":
                            EVENT_CHECK_INTERVAL = int.Parse(LineParts[2]);
                            break;
                        default: throw new ExceptionHandler("All values not set in trojan.cfg file : " + nameof(ConfigFile));
                    }
                }
            }
        }
    }
}
