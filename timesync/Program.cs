using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace timesync
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {


            bool canRun = false;
            System.Threading.Mutex mutex = new System.Threading.Mutex(true, "laiweishutdownapplication", out canRun);
            if (canRun)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1(args));
        

            }
            else
            {
                //Process currentProc = Process.GetCurrentProcess();
                //Process[] procs = Process.GetProcesses();
                //foreach(Process i in procs){
                //    if (i.Id != currentProc.Id && currentProc.ProcessName == i.ProcessName)
                //    {
                //         ShowWindowAsync(i.MainWindowHandle, 1);
                //         SetForegroundWindow(i.MainWindowHandle);
                //         break;
                //    }
                //}

                MessageBox.Show("程序已经在运行中!","提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
            

          
        }
     

        //[DllImport("User32.dll")]
        //private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        //[DllImport("User32.dll")]
        //private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
