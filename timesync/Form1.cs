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
namespace timesync
{
    public partial class Form1 : Form {
        // 是否后台启动模式
        private bool isStartOnBackEndMode = false;
        // 是否启动后立即同步一次时间
        private bool isStartAutoSyncTimeOnce = false;
        // 是否至少成功一次请求网络时间
        private bool fetchWebTimeOnceSuccess = false; 
        // 是否第一次最小化
        private bool isFirstMinWindow = true;
        // 同步间隔 - 分钟
        private decimal syncInterval;
        // 是否每隔指定周期自动同步
        private bool autoSyncCircle = false;
        // 是否在同步中
        private bool isFetching = false;
        // 标准时间和本地时间差
        private TimeSpan mTimeSpan;
        public delegate void AsyncDelegate (SyncType type, State state);
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
            readConfig();
            startTimer ();
            setWebTimeAsync (isStartAutoSyncTimeOnce);
            isStartOnBackEndMode = args.Length > 0;
            createFirstRunFile ();
            initTaskTimer ();
            runTaskTimer(this.syncInterval, this.autoSyncCircle);
            renderTime();

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
            setWebTimeAsync (true);
        }
        public void runTaskTimer (decimal syncInterval,bool enable) {
            taskTimer.Interval = (double) (syncInterval * 1000 * 60);
            taskTimer.Enabled = enable;
        }
        public enum State
        {
            pending = 1,
            success = 2,
            error = 0
        }
        public enum SyncType
        {
            get = 0,
            set = 1
        }
        public void setState (SyncType type, State state) {
            if (SyncType.set == type)
            {
                if (State.success == state)
                {
                    label4.Text = "同步成功";
                    label3.ForeColor = Color.FromArgb(0, 192, 0);
                    label4.ForeColor = Color.FromArgb(0, 192, 0);
                }
                else if (State.error == state)
                {
                    label4.Text = "同步失败";
                    label3.ForeColor = Color.Red;
                    label4.ForeColor = Color.Red;
                }
                else if (State.pending == state)
                {
                    label4.Text = "同步中...";
                    label3.ForeColor = Color.Gray;
                    label4.ForeColor = Color.Gray;
                }
            }
            else if (SyncType.get == type)
            {
                if (state == State.success || fetchWebTimeOnceSuccess)
                {
                    label1.ForeColor = Color.FromArgb(0, 192, 0);
                    label2.ForeColor = Color.FromArgb(0, 192, 0);
                }
                else if (state == State.pending)
                {
                    label1.ForeColor = Color.Gray;
                    label2.ForeColor = Color.Gray;
                    label2.Text = "获取中...";
                }

                else if (state == State.error)
                {
                    label2.Text = "获取失败";
                    label2.ForeColor = Color.Red;
                    label1.ForeColor = Color.Red;
                }
            }
           
           
        }
    
        public void setStateProxy(SyncType type,State state,Control control)
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke(new AsyncDelegate(setState), new object[] {type,state });
            } else
            {
                setState(type, state);
            }
        }
        private void startTimer () {
            System.Timers.Timer timer = new System.Timers.Timer ();
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += new System.Timers.ElapsedEventHandler (renderTime);
            timer.SynchronizingObject = this;
            timer.Enabled = true;
        }
        private void renderTime (object sender = null, System.Timers.ElapsedEventArgs e = null) {
            if (!isFetching)
            {
                label4.Text = DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss");
            }
            if (fetchWebTimeOnceSuccess) {
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
            string path = Assembly.GetExecutingAssembly ().Location;
            path = path.Substring(0, path.LastIndexOf('\\')) + @"\config.ini";
            INIClass ini = new INIClass (path);
            bool confirm = true;
            if (ini.ExistINIFile ()) {
                confirm = ini.IniReadValue ("EXIT", "exitConfirm", "1") == "1";
            }
            if (confirm)
            {
                Form3 exitForm = new Form3();
                exitForm.ShowDialog(this);
            }
            else
            {
                Dispose();
                Close();
                Application.Exit();
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
  

        private bool setAutoStart(bool start)
        {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName(appPath);
            bool success = true;
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (start)
                {
                    rk2.SetValue(appName, appPath + " -s");
                }
                else
                {
                    rk2.DeleteValue(appName, false);
                }
                rk2.Close();
                rk.Close();
                success = getAutoStartStatus() == start;
            }
            catch
            {
                success = false;
            }
            return success;
        }
        private bool getAutoStartStatus()
        {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName(appPath);
            object obj = Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run", appName, null);
            if (obj != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void registryAutoStart () {
          setAutoStart(true);
        }
      
        private void createFirstRunFile () {
            string firstRunFile = Assembly.GetExecutingAssembly ().Location;
            firstRunFile = firstRunFile.Substring (0, firstRunFile.LastIndexOf ('\\'))+ @"\firstrun.ini";
            if (!File.Exists (firstRunFile)) {
                FileStream NewText = File.Create (firstRunFile);
                NewText.Close ();
                new Thread (new ThreadStart (registryAutoStart)).Start ();
            }
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
            setStateProxy(SyncType.set, State.pending, label4);
            WebTime webtime = getWebTime ();
            bool success = false;
            if (webtime.status) {
                success = setTime (webtime.datetime);
                mTimeSpan = webtime.datetime - DateTime.Now;
                fetchWebTimeOnceSuccess = true;
            }
            setStateProxy(SyncType.get, webtime.status ? State.success : State.error, label2);
            setStateProxy(SyncType.set, success ? State.success : State.error, label4);
            System.Timers.Timer time = new System.Timers.Timer();
            time.AutoReset = false;
            time.Interval = 500;
            time.Elapsed += new System.Timers.ElapsedEventHandler(setDelay);
            time.SynchronizingObject = this;
            time.Enabled = true;
        }
        private void setDelay(object sender, System.Timers.ElapsedEventArgs e)
        {
            isFetching = false;
        }
        private void getWebTimeSteps () {
            WebTime webtime = getWebTime ();
            if (webtime.status) {
                mTimeSpan =  webtime.datetime - DateTime.Now;
                fetchWebTimeOnceSuccess = true;
            }
            setStateProxy(SyncType.get, webtime.status ? State.success : State.error, label2);
        }
        private void setWebTimeAsync (bool set = false) {
            Thread mThread;
            if(set){
               isFetching = true;
               mThread= new Thread (new ThreadStart (setWebTimeSteps));
            } else {
               mThread = new Thread (new ThreadStart (getWebTimeSteps));
            }
            mThread.Start ();
        }
        private void button1_Click (object sender, EventArgs e) {
            if (isFetching)
            {
                return;
            }
            setWebTimeAsync (true);
        }
        public struct WebTime {
            public DateTime datetime;
            public bool status;
        }

        private void readConfig()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            path = path.Substring(0, path.LastIndexOf('\\')) + @"\config.ini";
            INIClass ini = new INIClass(path);
            bool autoSyncCircle = ini.IniReadValue("AUTOSYNC", "autoSyncCircle", "1") == "1" ;
            isStartAutoSyncTimeOnce = ini.IniReadValue("AUTOSYNC", "autoSyncOnStart", "0") == "1";
            decimal syncInterval = int.Parse(ini.IniReadValue("AUTOSYNC", "interval", "5"));
            this.syncInterval = syncInterval;
            this.autoSyncCircle = autoSyncCircle;

        }
    }
}