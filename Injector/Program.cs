using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace Injector {
    internal class Program {
        public static void Main(string[] args) {
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "..\\..\\..\\WitcherHook\\bin\\Debug\\WitcherHook.dll");

            Debug.WriteLine("library "+injectionLibrary);
            Int32 targetPID = 0;

            string channelName = null;
            
            EasyHook.RemoteHooking.IpcCreateServer<ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);

            EasyHook.RemoteHooking.CreateAndInject(
                                                   "C:\\Program Files (x86)\\Steam\\steamapps\\common\\The Witcher 3\\bin\\x64\\witcher3.exe", // executable to run
                                                   "", // command line arguments for target
                                                   0, // additional process creation flags to pass to CreateProcess
                                                   EasyHook.InjectionOptions
                                                           .DoNotRequireStrongName, // allow injectionLibrary to be unsigned
                                                   injectionLibrary, // 32-bit library to inject (if target is 32-bit)
                                                   injectionLibrary, // 64-bit library to inject (if target is 64-bit)
                                                   out targetPID, // retrieve the newly created process ID
                                                   channelName // the parameters to pass into injected library
                                                  );
            // ...
            
            Console.WriteLine("<Press any key to exit>");
            Console.ResetColor();
            Console.ReadKey();
        }
    }

    public class ServerInterface : MarshalByRefObject
    {
        public void IsInstalled(int clientPID)
        {
            Console.WriteLine("FileMonitor has injected FileMonitorHook into process {0}.\r\n", clientPID);
            client = new NamedPipeClientStream(".", "WitcherSplit", PipeDirection.InOut, PipeOptions.Asynchronous);
            client.Connect();
        }

    
        public void ReportMessage(string message)
        {
            Console.WriteLine(message);
        }


        int count = 0;
        private NamedPipeClientStream client;

        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public void Ping()
        {
        }

        public void FactChanged(string factName, int factValue) {
            int len = factName.Length + 4;
            
            var buf = new byte[len+1];
            buf[0] = (byte) ((byte) len);

            for (int i = 0; i < factName.Length; i++) {
                buf[1 + i] = (byte) factName[i];
            }

            byte[] value = BitConverter.GetBytes(factValue);

            for (int i = 0; i < 4; i++) {
                buf[1 + factName.Length + i] = value[i];
            }

            client.WriteAsync(buf, 0, buf.Length);
        }
    }

}