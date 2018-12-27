using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;

namespace Injector {
    internal class Program {
        public static void Main(string[] args) {
            string witcherExe = "";
            
            
                string hookDll = ".\\WitcherHook.dll";
            
            if (args.Length < 1) {
                Debug.WriteLine("launch as injector.exe <pathToWitcher.exe> [pathToWitcherHook.dll]");
            } else {
                
                if (args.Length == 1) {
                    witcherExe = args[0];
                } else if (args.Length == 2) {
                    witcherExe = args[0];
                    hookDll = args[1];
                }
            }


            string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), hookDll);
            foreach (string s in args) {
                Console.WriteLine(s);
            }
            Console.WriteLine("library "+injectionLibrary);
            Console.WriteLine("witcher "+witcherExe);
            Int32 targetPID = 0;

            string channelName = null;
            
            EasyHook.RemoteHooking.IpcCreateServer<ServerInterface>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton);

            try {
                EasyHook.RemoteHooking.CreateAndInject(
                                                       witcherExe, // executable to run
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

            }
            catch (FileNotFoundException fileNotFoundException) {
                Console.WriteLine("Couldnt find file " + fileNotFoundException.FileName);
            }
            catch (Win32Exception win32Exception) {
                Console.WriteLine(win32Exception.NativeErrorCode);
            }

            Console.WriteLine("<Press any key to exit>");
            Console.ResetColor();
            Console.ReadKey();
        }
    }

    public class ServerInterface : MarshalByRefObject
    {
        public void IsInstalled(int clientPID)
        {
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

            var buf = new byte[len + 1];
            buf[0] = (byte) ((byte) len);

            for (int i = 0; i < factName.Length; i++) {
                buf[1 + i] = (byte) factName[i];
            }

            byte[] value = BitConverter.GetBytes(factValue);

            for (int i = 0; i < 4; i++) {
                buf[1 + factName.Length + i] = value[i];
            }

            try {
                client.WriteAsync(buf, 0, buf.Length);
            }
            catch (IOException e) {
                //Console.WriteLine(e);
            }
        }
    }

}