using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Trojan;
using client;
using Trojan.exceptions;

namespace SocketData.operations
{
    public class SocketOperations
    {
        public socketSetup Init_Socket(IPAddress address, socketSetup socketSetup)
        {
            socketSetup.setEndPoint(new IPEndPoint(address, socketSetup.port));
            socketSetup.sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            return socketSetup;
        }

        public void Bind_Socket(socketSetup socketSetup)
        {
            if (socketSetup.sock != null)
            {
                socketSetup.sock.Bind(socketSetup.getEndPoint());

                if (!socketSetup.sock.IsBound) throw new ExceptionHandler("[-] Socket failed to bind : " + nameof(socketSetup.sock));
            }
        }

        public void Listen_Socket(ref Socket sock, ushort max_connections)
        {
            if (sock.SocketType == SocketType.Dgram) throw new ExceptionHandler("[-] Socket cant listen on UDP ProtocolType, change to TCP : " + nameof(sock));

            if (sock.IsBound)
            {
                sock.Listen(max_connections);
            }
            else
            {
                throw new ExceptionHandler("[-] Socket is not bound to any address yet : " + nameof(sock));
            }
        }

        public async Task Accept_Socket(socketSetup socketSetup, ServerStatus SenderStatus)
        {
            while (true)
            {
                if (socketSetup.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(socketSetup.sock));
                if (socketSetup.sock.SocketType == SocketType.Dgram) throw new ExceptionHandler("[-] Socket cant accept connection on UDP ProtocolType, change to TCP : " + nameof(socketSetup.sock));

                if (!socketSetup.sock.Connected)
                {
                    Task<Socket> AcceptTask = socketSetup.sock.AcceptAsync();
                    Socket new_conn = AcceptTask.Result;

                    if (new_conn.RemoteEndPoint != null)
                    {
                        ConnectionManager.connections.Add(new ConnectionSetup(new_conn, new byte[SocketHandler.BUFFSIZE]));

                        if (AcceptTask.Result != null)
                        {
                            string introduce_message = "Hello, " + ConnectionManager.connections[ConnectionManager.connections.Count - 1].getRemoteEndPoint();

                            byte[] buffer = Encoding.UTF8.GetBytes(introduce_message);

                            Console.WriteLine("\r\n[+] Connection received from :: [{0}] ;", ConnectionManager.connections[ConnectionManager.connections.Count - 1].getRemoteEndPoint());
                            await ConnectionManager.connections[ConnectionManager.connections.Count - 1].getConn().SendAsync(buffer, SocketFlags.None);
                        }
                    }
                    else
                    {
                        throw new ExceptionHandler("[-] Connected socket can not be null : " + nameof(new_conn));
                    }
                }
                else
                {
                    socketSetup.sock.Close();

                    socketSetup = Init_Socket(IPAddress.Any, new socketSetup(Guid.NewGuid().ToString(), SocketHandler.PORT, SocketHandler.BUFFSIZE));

                    if (socketSetup.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(socketSetup.sock));

                    Bind_Socket(socketSetup);
                }
            }
        }

        public async Task Connect_Socket(socketSetup socketSetup)
        {
            if (socketSetup.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(socketSetup.sock));

            DnsEndPoint? hostEntry = new DnsEndPoint(socketSetup.getEndPoint().Address.ToString(), socketSetup.port);
            Task<Socket> TryConnectTask = socketSetup.sock.ConnectAsync(hostEntry).ContinueWith<Socket>(task =>
            {
                return task.IsFaulted ? null : socketSetup.sock;
            }, TaskContinuationOptions.ExecuteSynchronously);

            if (TryConnectTask == null) throw new ExceptionHandler("[-] ConnectTask can not be null : " + nameof(TryConnectTask));

            Task<Socket> timeoutTask = Task.Delay(SocketHandler.CONNECT_TIMEOUT).ContinueWith<Socket>(task => null, TaskContinuationOptions.ExecuteSynchronously);

            Task<Socket> resultTask = Task.WhenAny(TryConnectTask, timeoutTask).Unwrap();

            resultTask.Wait();

            Socket resultConnectTask = resultTask.Result;
            if (socketSetup.sock.Connected && resultConnectTask != null)
            {
                Client Client = new Client();
                await Client.Receive_Socket(socketSetup, SocketHandler.RECEIVE_TIMEOUT);
            }
            else
            {
                Console.WriteLine("\n\n[-] Socket failed to establish a connection with the server : " + nameof(resultConnectTask) + "\n\n");
            }
        }
    }
}
