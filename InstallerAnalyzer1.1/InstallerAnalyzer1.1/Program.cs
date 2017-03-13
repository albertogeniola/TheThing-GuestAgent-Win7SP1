using InstallerAnalyzer1_Guest.Properties;
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
                Microsoft.Win32.SystemEvents.SessionEnding += new Microsoft.Win32.SessionEndingEventHandler(SystemEvents_SessionEnded);

                Console.WriteLine("Bootstrap: Starting.");

                _xmlLog = new XmlDocument();
                _xmlRoot = _xmlLog.CreateElement("InstallerAnalyzerReport");
                _xmlLog.AppendChild(_xmlRoot);

                _xmlNative = _xmlLog.CreateElement("SyscallInvocations");
                _xmlRoot.AppendChild(_xmlNative);

                // Argument checking...
                #region Arguments checking
                if (args.Length != 2)
                    throw new ArgumentException("Missing arguments. Please specify HostControllerIP and Port.");
                
                try
                {
                    ProgramStatus.Instance.HostControllerAddr = Dns.GetHostAddresses(args[0]).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ElementAt(0);
                }
                catch (Exception e)
                {
                    throw new ApplicationException("It was impossible to get HOST_CONTROLLER_IP or HOST_CONTROLLER_IP is not valid. Please check internet connection first.");
                }

                try
                {
                    int _remotePort = int.Parse(args[1]);
                    if (_remotePort < 1 || _remotePort > 65535)
                        throw new ArgumentException("Invalid HOST_CONTROLLER_PORT Specified.");
                    else
                        ProgramStatus.Instance.HostControllerPort = _remotePort;
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Invalid HOST_CONTROLLER_PORT Specified.");
                }

                if (String.IsNullOrEmpty(Properties.Settings.Default.INJECTOR_IMAGE_NAME) || !File.Exists(Properties.Settings.Default.INJECTOR_IMAGE_NAME))
                {
                    throw new ApplicationException("Error: You must configure INJECTOR_PATH constant into the project to a valid path. I was unable to find \"" + Properties.Settings.Default.INJECTOR_IMAGE_NAME + "\" on this system.");
                }
                #endregion

                Console.WriteLine("Bootstrap: Args checks ok");
                // Visual Conf
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Prepare main window
                mw = new AnalyzerMainWindow(ProgramStatus.Instance.HostControllerAddr, ProgramStatus.Instance.HostControllerPort);
                mw.TopMost = true;

                Console.WriteLine("Bootstrap: Building logger");
                // Create and register the logger
                ProgramLogger.Instance.setTextBox(mw.getConsoleBox());
                Console.SetOut(ProgramLogger.Instance);
                Console.SetError(ProgramLogger.Instance);
                Console.ForegroundColor = ConsoleColor.Cyan;

                Console.WriteLine("Bootstrap: Starting NamedPipe Listener Thread");
                NamedPipeServer.Instance.Start();

                Console.WriteLine("Bootstrap: Running UI");
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

        static void SystemEvents_SessionEnded(object sender, SessionEndingEventArgs e)
        {
            // Abort shutdown.
            e.Cancel = true;
            Console.WriteLine("Detected Shutdown request. Ignored.");
        }

        
    }
}
