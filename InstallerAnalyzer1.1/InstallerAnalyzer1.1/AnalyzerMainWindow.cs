using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;


namespace InstallerAnalyzer1_Guest
{
    public partial class AnalyzerMainWindow : Form, IObserver<MonitoredProcesses>
    {
        private const int PATH_MAX_LEN = 260;
        private readonly static IntPtr MESSAGE_LOG = new IntPtr(0);
        private readonly static IntPtr MESSAGE_NEW_PROC = new IntPtr(1);
        private readonly static IntPtr MESSAGE_PROC_DIED = new IntPtr(2);
        private readonly static IntPtr MESSAGE_FILE_CREATED = new IntPtr(3);
        private readonly static IntPtr MESSAGE_FILE_DELETED = new IntPtr(4);
        private readonly static IntPtr MESSAGE_FILE_OPENED = new IntPtr(5);
        private readonly static IntPtr MESSAGE_FILE_RENAMED = new IntPtr(6);

        private readonly static IntPtr MESSAGE_REG_KEY_CREATED = new IntPtr(10);
        private readonly static IntPtr MESSAGE_REG_KEY_OPEN = new IntPtr(11);

        private LogicThread _t;
        private Timer _timer;
        private DateTime _startTime;

        public AnalyzerMainWindow(IPAddress ip, int port)
        {
            InitializeComponent();

            notifyIcon1.Visible = true;
            notifyIcon1.BalloonTipTitle="Program started.";
            notifyIcon1.BalloonTipText = "Connection to "+ip.ToString()+":"+port;
            notifyIcon1.ShowBalloonTip(10000);

            _t = new LogicThread(ip, port);
            this.Width = SystemInformation.PrimaryMonitorSize.Width;
            this.Shown += AnalyzerMainWindow_Shown;
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += _timer_Tick;

        }

        void _timer_Tick(object sender, EventArgs e)
        {
            string elapse = DateTime.Now.Subtract(_startTime).ToString(@"hh\:mm\:ss");

            // Update the timer on the UI
            if (InvokeRequired)
            {
                Invoke(new Action(()=>{
                    elapsedTime.Text = elapse;
                    logRateBox.Text = ""+ProgramStatus.Instance.LogsPerSec;
                    busy.Text = ProgramStatus.Instance.IsBusy() ? "Busy" : "";
                }));
            }
            else {
                elapsedTime.Text = elapse;
                logRateBox.Text = "" + ProgramStatus.Instance.LogsPerSec;
                busy.Text = ProgramStatus.Instance.IsBusy() ? "Busy" : "";
            }
        }

        void AnalyzerMainWindow_Shown(object sender, EventArgs e)
        {
            ProgramStatus.Instance.Subscribe(this);
            _t.Start();
            _startTime = DateTime.Now;
            _timer.Start();
        }

        
        public RichTextBox getConsoleBox()
        {
            return consoleBox;
        }

        
        public void OnNext(MonitoredProcesses pids)
        {
            var action = new Action(() =>
                {
                    monitoredPids.Text = "";
                    foreach (var pid in pids.processPids)
                        monitoredPids.Text += pid + ", ";

                    servicePids.Text = "";
                    foreach (var pid in pids.servicePids)
                        servicePids.Text += pid + ", ";

                });

            if (InvokeRequired)
                Invoke(action);
            else
                action();
            
        }

        private void monitoredPids_Click(object sender, EventArgs e)
        {

        }

        public void SetTimeoutExpired()
        {
            Action d = () =>
                {
                    timeout.Text = "TIMEOUT!";
                };

            if (InvokeRequired)
            {
                Invoke(d);
            }
            else {
                d();
            } 
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        private void servicePids_Click(object sender, EventArgs e)
        {

        }

        private void AnalyzerMainWindow_Shown_1(object sender, EventArgs e)
        {
            var wa = Screen.FromHandle(this.Handle).WorkingArea;
            NativeMethods.RECT r = new NativeMethods.RECT(0, this.Height, wa.Right, wa.Bottom);
            NativeMethods.SetWorkspace(r);


        }

        

    }
}
