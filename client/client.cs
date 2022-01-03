using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SocketData;
using TrojanExceptions;
namespace Trojan
{
    public class Client : SocketHandler
    {
        public socketSetup Connector;
        public socketSetup Connector2;
        public socketSetup Connector3;
        public socketSetup Connector4;
        public socketSetup Connector5;

        public async Task Send_Socket(Socket sock, string message, int SendTimeout)
        {
            if (sock == null) throw new ExceptionHandler("[-] Socket can not be null : " + nameof(socketSetup.sock));

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
                    await Send_Socket(socketSetup.sock, "Hello server from " + socketSetup.getUUID(), SEND_TIMEOUT);
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
            Connector = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));
            await _SocketOperations.Connect_Socket(Connector);
            Task ConnectorReceiver = Task.Factory.StartNew(() => Receive_Socket(Connector, Timeout.Infinite));

            Connector2 = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));
            await _SocketOperations.Connect_Socket(Connector2);
            Task Connector2Receiver = Task.Factory.StartNew(() => Receive_Socket(Connector2, Timeout.Infinite));

            Connector3 = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));
            await _SocketOperations.Connect_Socket(Connector3);
            Task Connector3Receiver = Task.Factory.StartNew(() => Receive_Socket(Connector3, Timeout.Infinite));

            Connector4 = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));
            await _SocketOperations.Connect_Socket(Connector4);
            Task Connector4Receiver = Task.Factory.StartNew(() => Receive_Socket(Connector4, Timeout.Infinite));


            Connector5 = _SocketOperations.Init_Socket(IPAddress.Parse("192.168.0.35"), new socketSetup(Guid.NewGuid().ToString(), port, buffSize));
            await _SocketOperations.Connect_Socket(Connector5);
            Task Connector5Receiver = Task.Factory.StartNew(() => Receive_Socket(Connector5, Timeout.Infinite));
        }
    }
}
