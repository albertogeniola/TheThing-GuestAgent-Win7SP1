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

/**
 * ==================== VERY IMPORTANT NOTE! ==================== 
 * Alberto Geniola, 03/02/2016
 * The Title of this window is used by the Injected DLL in order to send messages via SendMessage.
 * If we change the window name, this will be broken. At the moment the window name is WKWatcher.
 * If you change that, PLEASE UPDATE THIS NOTE and also the Injector/dll
** ==================== VERY IMPORTANT NOTE! ==================== */

namespace InstallerAnalyzer1_Guest
{
    public partial class AnalyzerMainWindow : Form, IObserver<uint[]>
    {
        private readonly static IntPtr MESSAGE_LOG = new IntPtr(0);
        private readonly static IntPtr MESSAGE_NEW_PROC = new IntPtr(1);
        private readonly static IntPtr MESSAGE_PROC_DIED = new IntPtr(2);
        private readonly static IntPtr MESSAGE_FILE_ACCESS = new IntPtr(3);
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
            string elapse = DateTime.Now.Subtract(_startTime).ToString();

            // Update the timer on the UI
            if (InvokeRequired)
            {
                Invoke(new Action(()=>{
                    elapsedTime.Text = elapse;
                }));
            }
            else {
                elapsedTime.Text = elapse;
            }
        }

        void AnalyzerMainWindow_Shown(object sender, EventArgs e)
        {
            ProgramStatus.Instance.Subscribe(this);
            _t.Start();
            _startTime = DateTime.Now;
            _timer.Start();
        }

        protected override void WndProc(ref Message m)
        {
            
            // Intercept CopyData messages
            if (m.Msg == 0x004A)
            {
                CopyDataStruct d = (CopyDataStruct)Marshal.PtrToStructure(m.LParam, typeof(CopyDataStruct));
                // Log data
                byte[] bb = new byte[d.cbData];
                for (int i = 0; i < bb.Length; i++)
                    bb[i] = Marshal.ReadByte(d.lpData, i);

                if (d.dwData == MESSAGE_LOG)
                {
                    // Let the program status we are receiving logs from the process.
                    // This can give us a hint about how hard are the background processes
                    // working on the system.
                    ProgramStatus.Instance.IncLogRate();
                    string s = Encoding.Unicode.GetString(bb);
                    AddRow(s);
                }
                else if (d.dwData == MESSAGE_NEW_PROC)
                {
                    // New process spawned
                    var pid = BitConverter.ToUInt32(bb, 0);
                    ProgramStatus.Instance.AddPid(pid);
                }
                else if (d.dwData == MESSAGE_PROC_DIED)
                {
                    // Process died
                    var pid = BitConverter.ToUInt32(bb, 0);
                    ProgramStatus.Instance.RemovePid(pid);
                }
                else if (d.dwData == MESSAGE_FILE_ACCESS)
                {
                    string s = Encoding.Unicode.GetString(bb);
                    ProgramStatus.Instance.NotifyFileAccess(s);
                }
            }
            base.WndProc(ref m);
            
        }


        private void AddRow(string r)
        {
            string row = null;
            try
            {
                row = r.Normalize();
            }
            catch (Exception e) {
                row = UnicodeEncoding.Unicode.GetString(UnicodeEncoding.Unicode.GetBytes(r));
            }
                
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(row);

                // TODO: is this really necessary?
                // Now encode base64 each value
                /*
                XmlNode el = doc.DocumentElement;
                foreach (XmlAttribute attr in el.Attributes)
                {
                    byte[] b = Encoding.UTF8.GetBytes(attr.Value);
                    attr.Value = Convert.ToBase64String(b);
                }
                */
                Program.appendXmlLog(doc.DocumentElement);
                logbox.Text = row;
            } catch (Exception e) {
                logbox.Text = "Error parsing." + row;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }


        public string getInstallerLog()
        {
            return logbox.Text;
        }

        public void resetInstallerLog()
        {
            logbox.Text = "";
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        public void appendInstallerLog(string xmlElement)
        {
            AddRow(xmlElement);
        }

        public RichTextBox getConsoleBox()
        {
            return this.consoleBox;
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(uint[] pids)
        {

            var action = new Action(() =>
                {
                    monitoredPids.Text = "";
                    foreach (var pid in pids)
                        monitoredPids.Text += pid + ", ";
                });

            if (InvokeRequired)
                Invoke(action);
            else
                action();
            
        }

        private void monitoredPids_Click(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
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
    }
}
