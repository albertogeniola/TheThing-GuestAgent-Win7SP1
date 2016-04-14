﻿using InstallerAnalyzer1_Guest.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace InstallerAnalyzer1_Guest
{
    class Program
    {
        private static AnalyzerMainWindow mw;
        private static XmlDocument _xmlLog;
        private static XmlElement _xmlRoot;
        private static XmlElement _xmlNative;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {

                IPAddress _remoteIp;
                int _remotePort;

                _xmlLog = new XmlDocument();
                _xmlRoot = _xmlLog.CreateElement("InstallerAnalyzerReport");
                _xmlLog.AppendChild(_xmlRoot);

                _xmlNative = _xmlLog.CreateElement("SyscallInvocations");
                _xmlRoot.AppendChild(_xmlNative);

                // Argument checking...
                #region Arguments checking

                if (String.IsNullOrEmpty(Properties.Settings.Default.INJECTOR_PATH) || !File.Exists(Properties.Settings.Default.INJECTOR_PATH))
                {
                    throw new ApplicationException("Error: You must configure INJECTOR_PATH constant into the project to a valid path. I was unable to find \"" + Properties.Settings.Default.INJECTOR_PATH + "\" on this system.");
                }

                if (String.IsNullOrEmpty(Properties.Settings.Default.HOST_CONTROLLER_IP) )
                {
                    throw new ApplicationException("Error: You must configure HOST_CONTROLLER_IP constant into the executable manifest/XML.");
                }

                if (String.IsNullOrEmpty(Properties.Settings.Default.HOST_CONTROLLER_PORT))
                {
                    throw new ApplicationException("Error: You must configure HOST_CONTROLLER_PORT constant into the executable manifest/XML.");
                }

                try
                {
                    _remoteIp = Dns.GetHostAddresses(Properties.Settings.Default.HOST_CONTROLLER_IP).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ElementAt(0);
                }
                catch (Exception e)
                {
                    throw new ApplicationException("It was impossible to get HOST_CONTROLLER_IP or HOST_CONTROLLER_IP is not valid. Please check internet connection first.");
                }

                try
                {
                    _remotePort = int.Parse(Properties.Settings.Default.HOST_CONTROLLER_PORT);
                    if (_remotePort < 1 || _remotePort > 65535)
                        throw new ArgumentException("Invalid HOST_CONTROLLER_PORT Specified.");
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Invalid HOST_CONTROLLER_PORT Specified.");
                }

                #endregion

                // Visual Conf
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Prepare main window
                mw = new AnalyzerMainWindow(_remoteIp, _remotePort);
                mw.TopMost = true;

                // Create and register the logger
                ProgramLogger.Instance.setTextBox(mw.getConsoleBox());
                Console.SetOut(ProgramLogger.Instance);
                Console.SetError(ProgramLogger.Instance);
                Console.ForegroundColor = ConsoleColor.Cyan;

                // Run the application
                Application.Run(mw);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message+Console.Error.NewLine+e.StackTrace);
                MessageBox.Show("UNHANDLED ERROR OCCURRED\nMessage:\n"+e.Message+"\nStackTrace:\n"+e.StackTrace);

                // TODO: need to reboot? What to do with the log file?
            }

        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            // This is necessary to close gracefully the logfile.
            ProgramLogger.Instance.Close();
        }

        public static void appendXmlLog(System.Xml.XmlElement xmlElement)
        {
            XmlNode node = _xmlLog.ImportNode(xmlElement,true);
            _xmlNative.AppendChild(node);
        }

        public static List<string> ListInstalledPrograms() {
            List<string> res = new List<string>();
            string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            using (Microsoft.Win32.RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        if (subkey.ValueCount > 0)
                        {
                            var value = subkey.GetValue("DisplayName");
                            if (value!=null)
                                res.Add(value.ToString());
                        }
                    }
                }
            }

            return res;
        }

        public static XmlElement GetInstallerLog()
        {
            return _xmlRoot;
        }

        public static void SetTimeoutExpired()
        {
            mw.SetTimeoutExpired();
        }
    }
}
