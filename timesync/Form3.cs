using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
namespace timesync {
    public partial class Form3 : Form {
        public Form3 () {
            InitializeComponent ();
        }
        private void button2_Click (object sender, EventArgs e) {
            Dispose ();
            Close ();
        }
        private void button1_Click (object sender, EventArgs e) {
            if (checkBox1.Checked) {
                string path = Assembly.GetExecutingAssembly ().Location;
                path = path.Substring(0, path.LastIndexOf('\\')) + @"\config.ini";
                new INIClass(path).IniWriteValue("EXIT", "exitConfirm", "0");
            }
            Close ();
            Dispose ();
            Application.ExitThread ();
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