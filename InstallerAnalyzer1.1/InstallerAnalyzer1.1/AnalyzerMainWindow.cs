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
        private readonly static IntPtr MESSAGE_FILE_CREATED = new IntPtr(3);
        private readonly static IntPtr MESSAGE_FILE_DELETED = new IntPtr(4);
        private readonly static IntPtr MESSAGE_FILE_OPENED = new IntPtr(5);
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
                    LogSyscall(s);
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
                else if (d.dwData == MESSAGE_FILE_CREATED || d.dwData == MESSAGE_FILE_OPENED || d.dwData == MESSAGE_FILE_DELETED)
                {
                    string s = Encoding.Unicode.GetString(bb);
                    ProgramStatus.Instance.NotifyFileAccess(s);
                }
                else if (d.dwData == MESSAGE_REG_KEY_OPEN || d.dwData == MESSAGE_REG_KEY_CREATED) {
                    try
                    {
                        string s = Encoding.Unicode.GetString(bb);
                        ProgramStatus.Instance.NotifyRegistryAccess(s);
                    }
                    catch (Exception e) {
                        var c = e.StackTrace;
                    }
                }
            }
            base.WndProc(ref m);
            
        }

        private int _seq = 0;
        private void LogSyscall(string xmlIn)
        {
            string row = null;
            try
            {
                row = xmlIn.Normalize();
            }
            catch (Exception e) {
                row = UnicodeEncoding.Unicode.GetString(UnicodeEncoding.Unicode.GetBytes(xmlIn));
            }
                
            
            // Load the XML
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(row);
            }
            catch (Exception e) {
                // This kind of error may happen when we receive invalid XML. Load it in base64
                // format for further investigation
                var sc = doc.CreateElement("Syscall");
                sc.SetAttribute("Method", "UNKNOWN");
                sc.SetAttribute("Sequence", _seq.ToString());
                doc.AppendChild(sc);
                
                // Encode in base 64 and add it to the raw data
                var rawData = doc.CreateElement("RawData");
                byte[] b = Encoding.UTF8.GetBytes(row);
                rawData.InnerText = Convert.ToBase64String(b);
                sc.AppendChild(rawData);

                _seq++;
                Program.appendXmlLog(sc);
                logbox.Text = row;
                return;
            }
                
            XmlElement root = doc.DocumentElement;
                
            // Now translate into a structured form
            string syscallName = root.Name;
            var el = doc.CreateElement("Syscall");
            el.SetAttribute("Method", syscallName);
            el.SetAttribute("Sequence", _seq.ToString());
            doc.RemoveChild(root);
            doc.AppendChild(el);

            // Add the syscall name
            var method = doc.CreateElement("Method");
            method.InnerText = syscallName;
            el.AppendChild(method);

            foreach (XmlAttribute attr in root.Attributes) {
                var child = doc.CreateElement(attr.Name);
                child.InnerText = attr.Value;
                el.AppendChild(child);
            }

            _seq++;
            Program.appendXmlLog(el);
            logbox.Text = row;
            
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
            LogSyscall(xmlElement);
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
