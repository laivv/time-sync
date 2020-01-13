using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace timesync {
    static class Program {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        /// 
        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        static void Main (string[] args) {
            bool runable = false;
            System.Threading.Mutex mutex = new System.Threading.Mutex(true, "lingluo-time-sync-2020-01-08-win32", out runable);

            if (!runable)
            {
                try
                {
                    IntPtr handle = getHandle("timesync-mapping");
                    ShowWindowAsync(handle, 1);
                    SetForegroundWindow(handle);
                    return;
                }
                catch (Exception e){ }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args));
        }
        static IntPtr getHandle(string key)
        {
            using (System.IO.MemoryMappedFiles.MemoryMappedFile mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(key))
            {
                using (System.IO.MemoryMappedFiles.MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                {
                    IntPtr handler = IntPtr.Zero;
                    accessor.Read(0, out handler);
                    return handler;
                }
            }
        }
    }
}