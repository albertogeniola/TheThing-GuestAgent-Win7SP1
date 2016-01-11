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
    public partial class AnalyzerMainWindow : Form
    {
        
        public AnalyzerMainWindow(IPAddress ip, int port)
        {
            InitializeComponent();
            notifyIcon1.BalloonTipTitle="Program started.";
            notifyIcon1.BalloonTipText = "Connection to "+ip.ToString()+":"+port;
            notifyIcon1.ShowBalloonTip(2000);
            LogicThread t = new LogicThread(ip, port);
            t.Start();
            this.Width = SystemInformation.PrimaryMonitorSize.Width;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x004A)
            {
                CopyDataStruct d = (CopyDataStruct)Marshal.PtrToStructure(m.LParam, typeof(CopyDataStruct));
                byte[] bb = new byte[d.cbData];
                for (int i = 0; i < bb.Length; i++)
                    bb[i] = Marshal.ReadByte(d.lpData, i);
                string s = Encoding.Unicode.GetString(bb);
                AddRow(s);                
            }
            base.WndProc(ref m);
        }


        private void AddRow(string row)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(row);

            // Now encode base64 each value
            XmlNode el = doc.DocumentElement;
            foreach (XmlAttribute attr in el.Attributes)
            {
                byte[] b = Encoding.UTF8.GetBytes(attr.Value);
                attr.Value = Convert.ToBase64String(b);
            }

            Program.appendXmlLog(doc.DocumentElement);
            logbox.AppendText(row + '\n');
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

        public void appendFollowedPath(string ids, string friendly)
        {
            this.followedPath.Text += ids + NetworkProtocol.Protocol.URI_PATH_SEP;
            this.userFriendlyPath.Text += friendly + NetworkProtocol.Protocol.URI_PATH_SEP;
        }
        
        public void appendInstallerLog(string xmlElement)
        {
            AddRow(xmlElement);
        }

        public RichTextBox getConsoleBox()
        {
            return this.consoleBox;
        }

        public void setSockAddr(string addr)
        {
            this.localSockAddr.Text = addr;
        }
    }
}
