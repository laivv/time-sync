using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace timesync {
    public partial class Form4 : Form {
        public Form4 () {
            InitializeComponent ();
            System.Timers.Timer time = new System.Timers.Timer ();
            time.AutoReset = false;
            time.Interval = 500;
            time.Elapsed += new System.Timers.ElapsedEventHandler (startwellcome);
            time.SynchronizingObject = this;
            time.Enabled = true;
        }
        private void startwellcome (object sender, System.Timers.ElapsedEventArgs e) {
            //if (this.progressBar1.Value == 100)
            //{
            //    (sender as System.Timers.Timer).Enabled = false;
            this.Close ();
            this.Dispose ();
            //    return;
            //}
            //this.progressBar1.Value += 20;
        }
    }
}