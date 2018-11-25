using System;
using System.Diagnostics;
using System.IO;

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
        }

        /// <summary>
        /// Output the message to the console.
        /// </summary>
        /// <param name="fileNames"></param>
        public void ReportMessages(string[] messages)
        {
            for (int i = 0; i < messages.Length; i++)
            {
                Console.WriteLine(messages[i]);
            }
        }

        public void ReportMessage(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Report exception
        /// </summary>
        /// <param name="e"></param>
        public void ReportException(Exception e)
        {
            Console.WriteLine("The target process has reported an error:\r\n" + e.ToString());
        }

        int count = 0;
        /// <summary>
        /// Called to confirm that the IPC channel is still open / host application has not closed
        /// </summary>
        public void Ping()
        {
        }
    }

}