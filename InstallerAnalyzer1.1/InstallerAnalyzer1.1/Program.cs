using InstallerAnalyzer1_Guest.Properties;
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
        private static ProgramLogger pl;
        private static XmlDocument _xmlLog;
        private static XmlElement _xmlRoot;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {

                IPAddress _remoteIp;
                int _remotePort;

                _xmlLog = new XmlDocument();
                _xmlRoot = _xmlLog.CreateElement("ROOT");
                _xmlLog.AppendChild(_xmlRoot);

                // Argument checking...
                #region Arguments checking
                // Read from the command line the server which connect to:
                if (args.Length != 2)
                {
                    throw new ArgumentException("Error: you must specify the server's IP and PORT as mondatory parameters\nSyntax:\nTestAutomationServer.exe 192.168.1.100 9000");
                }

                try
                {
                    _remoteIp = Dns.GetHostAddresses(args[0]).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ElementAt(0);
                    //_remoteIp = IPAddress.Parse(args[0]);
                }
                catch (Exception e)
                {
                    throw new ApplicationException("It was impossible to get ServerIP or server IP is not valid. Please check internet connection first.");
                }

                try
                {
                    _remotePort = int.Parse(args[1]);
                    if (_remotePort < 1 || _remotePort > 65535)
                        throw new ArgumentException("Invalid Port Specified.");
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Invalid Port Specified.");
                }

                if (String.IsNullOrEmpty(Properties.Settings.Default.INJECTOR_PATH) || !File.Exists(Properties.Settings.Default.INJECTOR_PATH))
                {
                    throw new ApplicationException("Error: You must configure INJECTOR_PATH constant into the project to a valid path. I was unable to find \"" + Properties.Settings.Default.INJECTOR_PATH + "\" on this system.");
                }

                if (String.IsNullOrEmpty(Properties.Settings.Default.DLL_PATH) || !File.Exists(Properties.Settings.Default.DLL_PATH))
                {
                    throw new ApplicationException("Error: You must configure DLL_PATH constant into the project to a valid path. I was unable to find \"" + Properties.Settings.Default.DLL_PATH + "\" on this system.");
                }

                #endregion

                // Visual Conf
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Prepare main window
                mw = new AnalyzerMainWindow(_remoteIp, _remotePort);
                mw.TopMost = true;

                // Create and register the logger
                pl = new ProgramLogger(Settings.Default.ApplicationLogFile);
                pl.setTextBox(mw.getConsoleBox());
                Console.SetOut(pl);
                Console.SetError(pl);
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
            pl.Close();
        }

        public static string GetMainWindowName()
        {
            return mw.Text;
        }

        public static XmlDocument GetInstallerLog()
        {
            if (mw == null)
                throw new ApplicationException("You have to start the application before getting the log!");
            else
            {
                return _xmlLog;
            }
                
        }


        public static void resetLog()
        {
            if (mw == null)
                throw new ApplicationException("You have to start the application before resetting the log!");
            else
            {
                _xmlLog = new XmlDocument();
                _xmlRoot = _xmlLog.CreateElement("ROOT");
                _xmlLog.AppendChild(_xmlRoot);

                mw.resetInstallerLog();
            }
        }
        
        internal static void appendInstallerLog(string xmlElement)
        {
            mw.appendInstallerLog(xmlElement);
        }

        public static void appendXmlLog(System.Xml.XmlElement xmlElement)
        {
            XmlNode node = _xmlLog.ImportNode(xmlElement,false);
            _xmlRoot.AppendChild(node);
        }
    }
}
