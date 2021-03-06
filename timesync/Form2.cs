﻿using System;
using System.Drawing;
using System.Windows.Forms;
namespace timesync
{
    public partial class Form2 : Form {
        public Form2 () {
            InitializeComponent ();
        }

        public static Form2 instance;
        public static void Open()
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
            instance = new Form2();
            instance.Show();
        }
        private void linkLabel1_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e) {
            System.Diagnostics.Process.Start ("http://www.ilaiv.com/timesync?v=1.1");
        }
        private void button1_Click (object sender, EventArgs e) {
            this.Close ();
            this.Dispose ();
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
                Location = new Point (LocationX + MousePos.X - MouseX, LocationY + MousePos.Y - MouseY);
            }
        }
        private void pictureBox2_Click (object sender, EventArgs e) {
            this.Close ();
            this.Dispose ();
        }
    }
}