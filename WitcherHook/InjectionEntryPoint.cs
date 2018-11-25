using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyHook;
using Injector;

namespace WitcherHook {
    public class InjectionEntryPoint: EasyHook.IEntryPoint
    {
        
        ServerInterface _server = null;
        private IntPtr factUpdateOrigPtr;


        public InjectionEntryPoint(
            EasyHook.RemoteHooking.IContext context,
            string channelName) {
            _server = EasyHook.RemoteHooking.IpcConnectClient<ServerInterface>(channelName);

            // If Ping fails then the Run method will be not be called
            _server.Ping();
            
        }
        
        public void Run(
            RemoteHooking.IContext context,
            string channelName) {
            _server.IsInstalled(RemoteHooking.GetCurrentProcessId());

            
            // 4c 89 4c 24 20 44 89 44 24 18 55 57 41
            
            factUpdateOrigPtr = Process.GetCurrentProcess().MainModule.BaseAddress+0x7b45f0;
            _server.ReportMessage(Process.GetCurrentProcess().MainModule.FileName);
            _server.ReportMessage(Process.GetCurrentProcess().MainModule.BaseAddress.ToString());
            _server.ReportMessage((Process.GetCurrentProcess().MainModule.BaseAddress+0x7b45f0).ToString());
            
            var hook = LocalHook.Create(factUpdateOrigPtr, new FactUpdateHook(FactUpdate_Hooked), this);
            
            
            hook.ThreadACL.SetExclusiveACL(new Int32[] { 0 });
            RemoteHooking.WakeUpProcess();

            
            _server.ReportMessage("done hooking");
            
            try
            {
                // Loop until FileMonitor closes (i.e. IPC fails)
                while (true)
                {
                    System.Threading.Thread.Sleep(500);

                   
                        _server.Ping();
                    
                }
            }
            catch
            {
                // Ping() or ReportMessages() will raise an exception if host is unreachable
            }

            // Remove hooks
            hook.Dispose();

            // Finalise cleanup of hooks
            EasyHook.LocalHook.Release();
        }
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U8)]
        delegate UInt64 FactUpdateHook(
            Int64 unknown,
            IntPtr factNamePtr,
            Int32 factValue,
            Int64 unknown2,
            Int64 validity);

        UInt64 FactUpdate_Hooked(
            Int64 unknown,
            IntPtr factNamePtr,
            Int32 factValue,
            Int64 unknown2,
            Int64 validity) {

            FactUpdateHook update = Marshal.GetDelegateForFunctionPointer<FactUpdateHook>(factUpdateOrigPtr);

            int len = Marshal.ReadInt32(factNamePtr + 8) -1;
            _server.ReportMessage("factNamePtr => " + factNamePtr);
            _server.ReportMessage("len => " + len);
            String factName = Marshal.PtrToStringUni(new IntPtr(Marshal.ReadInt64(factNamePtr)), len);
            
            
            _server.ReportMessage("called FactUpdate(factName=\"" + factName + "\",factValue=" + factValue + ",validity=" + validity + ")");

            _server.FactChanged(factName, factValue);
            
            return update(unknown, factNamePtr, factValue, unknown2, validity);
        }

    }


   
}