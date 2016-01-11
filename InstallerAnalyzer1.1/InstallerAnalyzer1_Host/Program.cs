using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using InstallerAnalyzer1_Host.Properties;

namespace InstallerAnalyzer1_Host
{
    class Program
    {
        private readonly static int VM_N = int.Parse(Settings.Default.VM_TO_SPAWN);
        public const string VBOX_BIN_PATH = @"C:\Program Files\Oracle\VirtualBox\VBoxManage.exe";
        private static DBManager man = DBManagerFactory.NewManager();

        static void Main(string[] args)
        {
            Console.CursorTop = 5;
            // Application Entrypoint: MAIN THREAD HERE
            // ------ END Debug purpose

            // 1.0 Initialization
            Log("Initializing...");
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(Dispose);
            if (Directory.Exists(Settings.Default.THREAD_LOGS_DIR))
                Directory.Delete(Settings.Default.THREAD_LOGS_DIR,true);
            // 2.0 Start Network Manager
            Log("Starting workermanager (Network Listener).");
            WorkerManager.Instance.Start();
            
            
            // 3.0 Start a pool of VMs
            Log("Creating VMS...");
            for (int i = 0; i < VM_N; i++)
            {
                VMManager.Instance.StartVm();
            }
            Log("VMs started. Waiting for bootup...");
            Stats.Instance.Start();
            // This should never exit: listening to network for free machine registration.
        }

        private static void Log(string p)
        {
            lock (Console.Out)
            {
                Console.WriteLine(p);
            }
        }



        public static string HashFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return HashFile(fs);
            }
        }

        public static string HashFile(FileStream stream)
        {
            StringBuilder sb = new StringBuilder();

            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);

                MD5 md5 = MD5CryptoServiceProvider.Create();
                byte[] hash = md5.ComputeHash(stream);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));

                stream.Seek(0, SeekOrigin.Begin);
            }

            return sb.ToString();
        }

        public static void Dispose(object sender, EventArgs e)
        {
            Log("Clearing resources...");
            man.Dispose();
            Stats.Instance.Stop();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            // This is necessary to close gracefully the logfile.
            Logger.Instance.Dispose();
        }
    }
}
