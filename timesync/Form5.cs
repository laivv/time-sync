using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
namespace timesync
{
    public partial class Form5 : Form {
        private Form1 parentForm;
        private INIClass ini;
        public Form5 (Form1 parentForm) {
            this.parentForm = parentForm;
            InitializeComponent ();
            initINI();
            renderView ();
        }
        public static Form5 instance;
        public static void Open(Form1 parentForm)
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form == instance)
                {
                    form.Activate();
                    form.WindowState = FormWindowState.Normal;
                    return;
                }
            }
            instance = new Form5(parentForm);
            instance.Show();
        }
        private struct CONFIG
        {
            public bool autoStart;
            public bool exitConfirm;
            public bool autoSyncOnStart;
            public bool autoSyncCircle;
            public decimal interval;
        }
        private void initINI()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            path = path.Substring(0, path.LastIndexOf('\\')) + @"\config.ini";
            INIClass ini = new INIClass(path);
            this.ini = ini;
        }
        private CONFIG getConfig()
        {
            CONFIG config;
            config.autoStart = getAutoStartStatus();
            config.exitConfirm = ini.IniReadValue("EXIT", "exitConfirm", "1") == "1";
            config.autoSyncOnStart = ini.IniReadValue("AUTOSYNC", "autoSyncOnStart", "0") == "1";
            config.autoSyncCircle = ini.IniReadValue("AUTOSYNC", "autoSyncCircle", "0") == "1";
            decimal interval = int.Parse(ini.IniReadValue("AUTOSYNC", "interval", "5"));
            decimal max = numericUpDown1.Maximum;
            decimal min = numericUpDown1.Minimum;
            interval = interval > max ? max : interval;
            interval = interval < min ? min : interval;
            config.interval = interval;
            return config;
        }
        private bool setAutoStart(bool start) {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName (appPath);
            bool success = true;
            try {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey (@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (start)
                {
                    rk2.SetValue(appName, appPath + " -s");
                } else
                {
                    rk2.DeleteValue(appName, false);
                }
                rk2.Close ();
                rk.Close ();
                success = getAutoStartStatus() == start;
            } catch {
                success = false;
            }
            return success;
        }
        private bool getAutoStartStatus () {
            string appPath = Application.ExecutablePath;
            string appName = System.IO.Path.GetFileName (appPath);
            object obj = Registry.GetValue (@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Run", appName, null);
            if (obj != null) {
                return true;
            } else {
                return false;
            }
        }
        private void renderView () {
            CONFIG config = getConfig();
            checkBox1.Checked = config.autoStart;
            checkBox2.Checked = config.exitConfirm;
            checkBox3.Checked = config.autoSyncOnStart;
            checkBox4.Checked = config.autoSyncCircle;
            numericUpDown1.Enabled = config.autoSyncCircle;
            numericUpDown1.Value = config.interval;
        }
        private void button1_Click (object sender, EventArgs e) {
            ini.IniWriteValue ("EXIT", "exitConfirm", checkBox2.Checked ? "1": "0");
            ini.IniWriteValue ("AUTOSYNC", "autoSyncOnStart", checkBox3.Checked ? "1":"0");
            ini.IniWriteValue("AUTOSYNC", "autoSyncCircle", checkBox4.Checked ? "1" : "0");
            ini.IniWriteValue("AUTOSYNC", "interval", numericUpDown1.Value.ToString());
            parentForm.runTaskTimer(numericUpDown1.Value, checkBox4.Checked);
            if (getAutoStartStatus()!= checkBox1.Checked && !setAutoStart(checkBox1.Checked)) {
                renderView ();
                MessageBox.Show ("部分设置保存失败！", "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else {
                MessageBox.Show ("设置保存成功！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close ();
                Dispose ();
            }
        }
     
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = checkBox4.Checked;
        }
        private void pictureBox2_Click (object sender, EventArgs e) {
            Close ();
            Dispose ();
        }
        private void checkBox1_Click (object sender, EventArgs e) {
            if (checkBox1.Checked == false) {
                MessageBox.Show ("取消开机启动将不能在开机时同步时间！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
    }
}