using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
namespace timesync {
    public partial class Form1 : Form {
        // 是否后台启动模式
        private bool isStartOnBackEndMode = false;
        // 是否启动后立即同步一次时间
        private bool isStartAutoSyncTimeOnce = false;
        // 是否至少成功一次请求网络时间
        private bool isOnceSuccessGetInternetTime = false; 
        // 是否第一次最小化
        private bool isFirstMinWindow = true;
        // 同步间隔 - 分钟
        private decimal syncInterval;
        // 是否每隔指定周期自动同步
        private bool isAutoSync = false;
        // 是否要刷新显示本机时间
        private bool isDelay = false;
        private TimeSpan mTimeSpan;
        public delegate void Delegator ();
        public Delegator mMyDelegate;
        public Delegator mMyDelegateSuccess;
        public Delegator syncSuccess;
        public Delegator syncError;
        public Delegator syncPending;
        private System.Timers.Timer taskTimer = new System.Timers.Timer ();
        private string[] ntps = new string[] {
            "ntp1.aliyun.com",
            "time.windows.com",
            "ntp.ntsc.ac.cn",
            "ntp2.aliyun.com",
            "ntp3.aliyun.com",
            "ntp4.aliyun.com",
            "ntp5.aliyun.com",
            "ntp6.aliyun.com",
            "ntp7.aliyun.com",
            "cn.pool.ntp.org",
            "cn.ntp.org.cn"
        };
        private int nptIndex = 0;
        public Form1 (string[] args) {
            InitializeComponent ();
            init (args);
        }
        private void init (string[] args) {
            
            readSyncConfig();
            mMyDelegate = new Delegator (setWebTimeTextError);
            mMyDelegateSuccess = new Delegator (setWebTimeTextSuccess);
            syncSuccess = new Delegator (setSyncSatusSuccess);
            syncError = new Delegator (setSyncStatusError);
            syncPending = new Delegator (setSyncStatusPending);
            startTimer ();
            if (isStartAutoSyncTimeOnce) {
                setWebTimeAsync ();
            } else {
                getWebTimeAsync ();
            }
            isStartOnBackEndMode = args.Length > 0;
            setAutoStart ();
            initTaskTimer ();
            runTaskTimer(this.syncInterval, this.isAutoSync);
            if (!isDelay)
            {
                label4.Text = DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
            }

        }
        public struct SYSTEMTIME {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
            public void FromDateTime (DateTime time) {
                wYear = (ushort) time.Year;
                wMonth = (ushort) time.Month;
                wDayOfWeek = (ushort) time.DayOfWeek;
                wDay = (ushort) time.Day;
                wHour = (ushort) time.Hour;
                wMinute = (ushort) time.Minute;
                wSecond = (ushort) time.Second;
                wMilliseconds = (ushort) time.Millisecond;
            }
        }
        [DllImport ("Kernel32.dll")]
        public static extern bool SetLocalTime (ref SYSTEMTIME Time);
        private bool setTime (DateTime datetime) {
            SYSTEMTIME st = new SYSTEMTIME ();
            st.FromDateTime (datetime);
            if (SetLocalTime (ref st)) {
                mTimeSpan = DateTime.Now - DateTime.Now;
                return true;
            }
            return false;
        }
        private void initTaskTimer () {
            taskTimer.AutoReset = true;
            taskTimer.Elapsed += new System.Timers.ElapsedEventHandler (taskSyncTime);
            taskTimer.SynchronizingObject = this;
        }
        private void taskSyncTime (object sender, System.Timers.ElapsedEventArgs e) {
            setWebTimeAsync ();
        }
        public void runTaskTimer (decimal syncInterval,bool enable) {
            taskTimer.Interval = (double) (syncInterval * 1000 * 60);
            taskTimer.Enabled = enable;
        }
        public void setSyncSatusSuccess () {
            //label7.Text = "同步成功";
            //label7.ForeColor = Color.FromArgb (0, 192, 0);
            label4.Text = "同步成功";
            label3.ForeColor = Color.FromArgb(0, 192, 0);
            label4.ForeColor = Color.FromArgb (0, 192, 0);
        }
        public void setSyncStatusError () {
            //label7.Text = "同步失败";
            //label7.ForeColor = Color.Red;
            label4.Text = "同步失败";
            label3.ForeColor = Color.Red;
            label4.ForeColor = Color.Red;
        }
        public void setSyncStatusPending () {
            //label7.Text = "同步中...";
            //label7.ForeColor = Color.FromArgb (0, 192, 0);
            label4.Text = "同步中...";
            label3.ForeColor = Color.Gray;
            label4.ForeColor = Color.Gray;
          
        }
        public void setWebTimeTextError () {
            label2.Text = "获取失败";
            label2.ForeColor = Color.Red;
            label1.ForeColor = Color.Red;
        }
        public void setWebTimeTextSuccess () {
            label1.ForeColor = Color.FromArgb (0, 192, 0);
            label2.ForeColor = Color.FromArgb (0, 192, 0);
        }
        public void setWebTimeTextLoading () {
            if (!isOnceSuccessGetInternetTime)
            {
                label1.ForeColor = Color.Gray;
                label2.ForeColor = Color.Gray;
                label2.Text = "获取中...";
            }
        }
    
        private void startTimer () {
            System.Timers.Timer timer = new System.Timers.Timer ();
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler (refreshTime);
            timer.SynchronizingObject = this;
            timer.Enabled = true;
        }
        private void refreshTime (object sender, System.Timers.ElapsedEventArgs e) {
            if (!isDelay)
            {
                label4.Text = DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
            }
            if (isOnceSuccessGetInternetTime) {
                label2.Text = DateTime.Now.AddSeconds (mTimeSpan.TotalSeconds).ToString ("yyyy-MM-dd HH:mm:ss");
            }
        }
     
        private void minWindow () {
            this.Hide ();
            if (isFirstMinWindow) {
                this.notifyIcon1.ShowBalloonTip (2000, "", "时间同步-最小化在此处", ToolTipIcon.Info);
                isFirstMinWindow = false;
            }
        }
        private void showWindow () {
            this.Show ();
            if (this.WindowState == FormWindowState.Minimized) {
                this.WindowState = FormWindowState.Normal;
            }
        }
        private void Form1_FormClosing (object sender, FormClosingEventArgs e) {
            e.Cancel = true;
            minWindow ();
        }
        private void notifyIcon1_MouseClick (object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                showWindow ();
            }
        }
        private void ToolStripMenuItem1_Click (object sender, EventArgs e) {
            string stmp = Assembly.GetExecutingAssembly ().Location;
            stmp = stmp.Substring (0, stmp.LastIndexOf ('\\'));
            INIClass ini = new INIClass (stmp + @"\config.ini");
            if (ini.ExistINIFile ()) {
                string isConfirm = ini.IniReadValue ("EXIT", "confirm", "1");
                if (isConfirm == "1") {
                    Form3 exitForm = new Form3 ();
                    exitForm.ShowDialog (this);
                } else {
                    this.Dispose ();
                    this.Close ();
                    Application.Exit ();
                }
            } else {
                Form3 exitForm = new Form3 ();
                exitForm.ShowDialog (this);
            }
        }
        private void ToolStripMenuItem_Click (object sender, EventArgs e) {
            Form2 childForm = new Form2 ();
            childForm.Show ();
        }
        private void ToolStripMenuItem2_Click (object sender, EventArgs e) {
            showWindow ();
        }
        private bool isEnterMenuPanel = false;
        private bool isMouseDown = false;
        private int MouseX, MouseY, LocationX, LocationY;
        private void panel3_MouseEnter (object sender, EventArgs e) {
            isEnterMenuPanel = true;
        }
        private void panel3_MouseLeave (object sender, EventArgs e) {
            isEnterMenuPanel = false;
        }
        private void panel3_MouseDown (object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                isMouseDown = true;
                Point MousePos = Control.MousePosition;
                MouseX = MousePos.X;
                MouseY = MousePos.Y;
                LocationX = this.Location.X;
                LocationY = this.Location.Y;
            }
        }
        private void panel3_MouseUp (object sender, MouseEventArgs e) {
            isMouseDown = false;
        }
        private void panel3_MouseMove (object sender, MouseEventArgs e) {
            if (isMouseDown && isEnterMenuPanel) {
                Point MousePos = Control.MousePosition;
                this.Location = new Point (LocationX + MousePos.X - MouseX, LocationY + MousePos.Y - MouseY);
            }
        }
        private void pictureBox1_Click (object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.WindowState = FormWindowState.Minimized;
            }
        }
        private void pictureBox2_Click (object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                minWindow ();
            }
        }
        private void Form1_Load (object sender, EventArgs e) {
            if (!isStartOnBackEndMode) {
                //显示启动界面
                //Form4 wellcome = new Form4();
                //wellcome.ShowDialog();
            }
        }
        private void openAutoStart (object sender = null, System.Timers.ElapsedEventArgs e = null) {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName (appPath);
            try {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey (@"Software\Microsoft\Windows\CurrentVersion\Run");
                rk2.SetValue (appName, appPath + " -s");
                rk2.Close ();
                rk.Close ();
            } catch { }
        }
        private bool checkAutorunStatus () {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName (appPath);
            Object obj = Registry.GetValue (@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run", appName, null);
            if (obj != null) {
                return true;
            } else {
                return false;
            }
        }
        private void closeAutoStart () {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName (appPath);
            RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk2 = rk.CreateSubKey (@"Software\Microsoft\Windows\CurrentVersion\Run");
            rk2.DeleteValue (appName, false);
            rk2.Close ();
            rk.Close ();
        }
        private void setAutoStart () {
            string stmp = Assembly.GetExecutingAssembly ().Location;
            stmp = stmp.Substring (0, stmp.LastIndexOf ('\\'));
            string firstRunFile = stmp + @"\firstrun.ini";
            if (!File.Exists (firstRunFile)) {
                FileStream NewText = File.Create (firstRunFile);
                NewText.Close ();
                new Thread (new ThreadStart (registerAutoStart)).Start ();
            }
        }
        private void registerAutoStart () {
            System.Timers.Timer timer = new System.Timers.Timer ();
            timer.AutoReset = false;
            timer.Interval = 10 * 1000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler (openAutoStart);
            timer.SynchronizingObject = this;
            timer.Enabled = true;
        }
        private void Form1_Shown (object sender, EventArgs e) {
            if (isStartOnBackEndMode) {
                this.Hide ();
            }
        }
        private void ToolStripMenuItem_Click_Setting (object sender, EventArgs e) {
            Form5 setting = new Form5 (this);
            setting.Show ();
        }
        public WebTime getWebTime () {
            WebTime webtime;
            webtime.datetime = DateTime.Now;
            webtime.status = false;
            int len = ntps.Length;
            for (int i = 0; i < len ; i++) {
                string ntpServer = ntps[nptIndex];
                nptIndex++;
                nptIndex = nptIndex >= len ? 0 : nptIndex;
                Console.WriteLine(ntpServer);
                byte[] ntpData = new byte[48];
                ntpData[0] = 0x1B;
                Socket socket = null;
                try {
                    IPAddress[] addresses = Dns.GetHostEntry (ntpServer).AddressList;
                    IPEndPoint ipEndPoint = new IPEndPoint (addresses[0], 123);
                    socket = new Socket (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Connect (ipEndPoint);
                    socket.ReceiveTimeout = 3000;
                    socket.Send (ntpData);
                    socket.Receive (ntpData);
                    socket.Close ();
                    const byte serverReplyTime = 40;
                    ulong intPart = BitConverter.ToUInt32 (ntpData, serverReplyTime);
                    ulong fractPart = BitConverter.ToUInt32 (ntpData, serverReplyTime + 4);
                    intPart = swapEndian (intPart);
                    fractPart = swapEndian (fractPart);
                    ulong milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000UL);
                    DateTime webTime = (new DateTime (1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds (milliseconds);
                    webtime.datetime = webTime.ToLocalTime ();
                    webtime.status = true;
                    break;
                } catch (Exception e) {
                    if (socket != null) {
                        socket.Close ();
                    }
                }
                
            }
            return webtime;
        }
        private uint swapEndian (ulong x) {
            return (uint) (((x & 0x000000ff) << 24) +
                ((x & 0x0000ff00) << 8) +
                ((x & 0x00ff0000) >> 8) +
                ((x & 0xff000000) >> 24));
        }
        private void setWebTimeSteps () {
            if (label4.InvokeRequired) {
                label4.BeginInvoke (syncPending);
            } else {
                setSyncStatusPending ();
            }
            WebTime webtime = getWebTime ();
            if (webtime.status) {
                bool isSuccess = setTime (webtime.datetime);
                mTimeSpan = webtime.datetime - DateTime.Now;
                isOnceSuccessGetInternetTime = true;
                if (label2.InvokeRequired) {
                    label2.BeginInvoke (mMyDelegateSuccess);
                } else {
                    setWebTimeTextSuccess ();
                }

               
            
        
                if (isSuccess) {
                    if (label4.InvokeRequired) {
                        label4.BeginInvoke (syncSuccess);
                    } else {
                        setSyncSatusSuccess ();
                    }
                } else {
                    if (label4.InvokeRequired) {
                        label4.BeginInvoke (syncError);
                    } else {
                        setSyncStatusError ();
                    }
                }
            } else if (!isOnceSuccessGetInternetTime) {
                if (label2.InvokeRequired) {
                    label2.BeginInvoke (mMyDelegate);
                } else {
                    setWebTimeTextError ();
                }
            }
            if (!webtime.status) {
                if (label4.InvokeRequired) {
                    label4.BeginInvoke (syncError);
                } else {
                    setSyncStatusError ();
                }
            }
             System.Timers.Timer time = new System.Timers.Timer();
                time.AutoReset = false;
                time.Interval = 500;
                time.Elapsed += new System.Timers.ElapsedEventHandler(setDelay);
                time.SynchronizingObject = this;
                time.Enabled = true;
        }
        private void setDelay(object sender, System.Timers.ElapsedEventArgs e)
        {
            isDelay = false;
        }
        private void setWebTimeAsync () {
            isDelay = true;
            Thread mThread = new Thread (new ThreadStart (setWebTimeSteps));
            mThread.Start ();
        }
        private void getWebTimeSteps () {
            WebTime webtime = getWebTime ();
            if (webtime.status) {
                mTimeSpan =  webtime.datetime - DateTime.Now;
                isOnceSuccessGetInternetTime = true;
                if (label2.InvokeRequired) {
                    label2.BeginInvoke (mMyDelegateSuccess);
                } else {
                    setWebTimeTextSuccess ();
                }
            } else if (!isOnceSuccessGetInternetTime) {
                if (label2.InvokeRequired) {
                    label2.BeginInvoke (mMyDelegate);
                } else {
                    setWebTimeTextError ();
                }
            }
        }
        private void getWebTimeAsync () {
            Thread mThread = new Thread (new ThreadStart (getWebTimeSteps));
            mThread.Start ();
        }
        private void button1_Click (object sender, EventArgs e) {
            setWebTimeTextLoading ();
            setWebTimeAsync ();
        }
        public struct WebTime {
            public DateTime datetime;
            public bool status;
        }

        private void readSyncConfig()
        {
            string stmp = Assembly.GetExecutingAssembly().Location;
            stmp = stmp.Substring(0, stmp.LastIndexOf('\\'));
            INIClass ini = new INIClass(stmp + @"\config.ini");
            bool isAutoSync = ini.IniReadValue("AUTOSYNC", "enable", "1") == "1" ? true : false;
            isStartAutoSyncTimeOnce = ini.IniReadValue("AUTOSYNC", "startsync", "0") == "1" ? true : false;
            decimal syncInterval = int.Parse(ini.IniReadValue("AUTOSYNC", "interval", "5"));
            this.syncInterval = syncInterval;
            this.isAutoSync = isAutoSync;

        }
    }
}