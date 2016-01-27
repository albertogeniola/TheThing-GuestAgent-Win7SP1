﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkProtocol;
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Xml;
using InstallerAnalyzer1_Guest.Properties;
using System.Windows;
using System.Net.NetworkInformation;
using InstallerAnalyzer1_Guest.Protocol;
using Newtonsoft.Json;

namespace InstallerAnalyzer1_Guest
{
    class LogicThread
    {
        const int ACQUIRE_WORK_SLEEP_SECS = 10;
        readonly string DEFAULT_REPORT_PATH = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "report.xml");

        private int _getWorkPollingTime = 5000; // Poll every 5 secs
        
        // Network objects
        private IPAddress _remoteIp;
        private int _remotePort;
        private string _mac;

        #region Network Methods
        private TcpClient ConnectToHost()
        {
            TcpClient _tcpEndpoint = null;
            while (_tcpEndpoint == null || !_tcpEndpoint.Connected)
            {
                try
                {
                    _tcpEndpoint = new TcpClient(_remoteIp.ToString(), _remotePort);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    _tcpEndpoint = null;
                    Console.WriteLine(String.Format("The remote node <{0}:{1}> seems to be down. Waiting...", _remoteIp, _remotePort));
                    Thread.Sleep(5000);
                }
            }
            Console.WriteLine("Connected to " + _remoteIp + " : " + _remotePort);
            return _tcpEndpoint;

        }
        private void __send(NetworkStream ns, byte[] data) {
            ns.Write(data, 0, data.Length);
        }
        private void __recv(NetworkStream ns, ref byte[] buf)
        {
            ns.Read(buf, 0, buf.Length);
        }
        private void __recvFile(NetworkStream ns, FileStream fs, int dim)
        {
            byte[] buff = new byte[8192];
            int tot = 0;

            while (tot < dim) {
                int toread = ((dim - tot) > buff.Length ) ? buff.Length : (dim - tot);
                int r = ns.Read(buff, 0, toread);
                fs.Write(buff, 0, r);
                tot += r;
            }
        }
        private void __sendFile(NetworkStream ns, FileStream fs, int dim)
        {
            byte[] buff = new byte[8192];
            long written = 0;
            while (written < dim)
            {
                int read = fs.Read(buff, 0, buff.Length);
                ns.Write(buff, 0, read);
                written += read;
            }
        }
        private void _send_message(NetworkStream ns, string msg) { 
            // Encode the string and get its byte length
            var utf8 = Encoding.UTF8;
            byte[] utfBytes = utf8.GetBytes(msg);

            // Convert byte length to network unsigned int
            int len = IPAddress.HostToNetworkOrder(utfBytes.Length);

            // now send the len
            __send(ns,BitConverter.GetBytes(len));

            // finally send binary data
            __send(ns,utfBytes);

        }
        private string _recv_message(NetworkStream ns)
        {
            byte[] i = new byte[4];
            // Read the length
            __recv(ns, ref i);

            UInt32 nlen = BitConverter.ToUInt32(i, 0);
            int len = IPAddress.NetworkToHostOrder((int)nlen);
            
            // Now read the rest of message
            byte[] raw = new byte[len];
            __recv(ns, ref raw);
            var utf8 = Encoding.UTF8;
            return utf8.GetString(raw);
        }
        #endregion

        #region Communication with HostController
        private ResponseGetWork RequestWork(NetworkStream ns)
        {
            RequestGetWork req = new RequestGetWork();
            req.Mac = _mac;
            _send_message(ns,JsonConvert.SerializeObject(req));
            return JsonConvert.DeserializeObject<ResponseGetWork>(_recv_message(ns));
        }

        private ResponseReportWork SendReport(NetworkStream ns, long workId, long reportFileLen, string status)
        {
            RequestReportWork r = new RequestReportWork();
            r.Mac = _mac;
            r.ReportLenInBytes = reportFileLen;
            r.WorkId = workId;
            r.Status = status;

            _send_message(ns, JsonConvert.SerializeObject(r));
            return JsonConvert.DeserializeObject<ResponseReportWork>(_recv_message(ns));
        }
        private ResponseReportWorkReportReceived WaitServerReportAck(NetworkStream ns)
        {
            return JsonConvert.DeserializeObject<ResponseReportWorkReportReceived>(_recv_message(ns));
        }
        #endregion

        private void ReportWork(Job j, string reportFilePath, string status)
        {
            var client = ConnectToHost();
            try
            {
                Console.WriteLine("Reporting job completition to Host Controller");
                var ns = client.GetStream();

                string err;
                
                // We will need file dimension and work id to perform a report
                FileInfo f = new FileInfo(reportFilePath);

                // Send the report info to the server
                ResponseReportWork res = SendReport(ns, j.Id, f.Length, status);

                if (!res.isValid(out err))
                    //TODO deal with response in case is invalid
                    throw new ProtocolException("Received response by Host Controller was not valid. " + err);

                // Time to send the report to the server
                using (var fs = File.OpenRead(reportFilePath)) {
                    __sendFile(ns, fs, (int)f.Length);
                }
                
                // Did the server receive the file correctly?
                ResponseReportWorkReportReceived rr = WaitServerReportAck(ns);
                if (!rr.isValid(out err))
                    //TODO deal with NACK
                    throw new ProtocolException("Received response by Host Controller was not valid. " + err);
                
                // Done!
            }
            finally
            {
                client.Close();
            }
        }

        
        /// <summary>
        /// This function connects to the Host controller and asks for a job to be executed locally.
        /// In case there are jobs to be executed, this method also takes care of installer download.
        /// If no jobs have to be performed, this method return null.
        /// Please note that calling this method may block the caller because file transfer and network
        /// operations may happen.
        /// </summary>
        /// <returns>A Job or null in case no job is available.</returns>
        private Job AcquireWork()
        {
            // Connect to the remote host  
            var client = ConnectToHost();
            try
            {
                Console.WriteLine("Requesting a job from Host Controller");
                var ns = client.GetStream();

                string err;
                // Send a RequestGetWork
                var res = RequestWork(ns);
                if (!res.isValid(out err))
                    throw new ProtocolException("Received response by Host Controller was not valid. " + err);

                if (res.WorkId == null)
                {
                    // This means the server has nothing to do atm. Let the caller decide what to do.
                    return null;
                }

                // Let's get the data from the server. We need to allocate the space locally and then send an ACK
                // to the server, so it will start outputting data.
                string path = Path.Combine(System.IO.Path.GetTempPath(), res.FileName);
                FileStream fs = File.Create(path);
                try
                {
                    fs.SetLength(res.FileDim);
                    // Now we send an ACK to the HostController, so it starts sending data through the socket
                    RequestGetWorkFile r = new RequestGetWorkFile();
                    _send_message(ns, JsonConvert.SerializeObject(r));
                    __recvFile(ns, fs, (int)res.FileDim);
                }
                catch (IOException e)
                {
                    // If an error occurs, drop the file and let the parent handle the exception.
                    fs.SetLength(0);
                    fs.Close();
                    File.Delete(path);

                    throw e;
                }

                fs.Close();
                fs.Dispose();

                // File received. Let the HostController we will work on it
                RequestGetWorkFileReceived rr = new RequestGetWorkFileReceived();
                _send_message(ns, JsonConvert.SerializeObject(rr));

                // In case there was nothing to do, the server responds with a null work_id. We handle this possibility here, by returning
                // null in case it happens.
                Job j = null;
                if (res.WorkId !=null)
                    j = new Job((long)res.WorkId, path);

                return j;
            }
            finally
            {
                client.Close();
            }

        }

        private ProcessContainer ExecuteJob(Job j, InteractionPolicy policy) {
            // Start the process by injecting our DLL pointed by the settings file.
            var proc = StartProcessWithInjector(j);
            Console.WriteLine("Installer started.");

            // Continue until the process exits.
            while (!proc.Process.HasExited)
            {
                Console.WriteLine("Installer process still running...");
                Window waitingWindnow = null;
                
                // Wait for inputrequested
                try
                {
                    Console.WriteLine("Waiting for windows to be stable...");
                    waitingWindnow = WaitForInputRequested(proc);
                    Console.WriteLine("Ok, ready for user input. ");
                }
                catch (ProcessExitedException e)
                {
                    break;
                }

                // Now let the interaction happen. The way our monkey chooses the buttons to press
                // depends on the policy passed as argument.
                policy.Interact(waitingWindnow);

                // At this point we reiterate again, untile the pocess runs.
            }

            return proc;
        }

        public LogicThread(IPAddress remoteIp, int remotePort)
        {
            _remoteIp = remoteIp;
            _remotePort = remotePort;
            _mac = GetMACAddr();
        }

        public void Start()
        {
            Thread t = new Thread(new ThreadStart(work));
            t.Start();
        }

        /// <summary>
        /// This method represents the entry point for the thread work.
        /// </summary>
        private void work()
        {
            InteractionPolicy policy = new BasicInteractionPolicy();
            bool keepRunning = true;
            try
            {
                while (keepRunning)
                {
                    // Acquire the job from the server.
                    // This may return null in case the server has nothing to do.
                    Job j = AcquireWork();
                    
                    // If there is nothing to do, sleep and run again. Otherwise keep going.
                    if (j == null)
                    {
                        Console.WriteLine("There is nothing to do at the moment. We sleep and retry in " + ACQUIRE_WORK_SLEEP_SECS + " seconds.");
                        Thread.Sleep(ACQUIRE_WORK_SLEEP_SECS * 1000);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Job acquired from controller. Job ID {0}, file name {1}.", j.Id, j.LocalFullPath));
                    }

                    // Time to play with the installer!
                    var proc = ExecuteJob(j, policy);
                    Console.WriteLine("Installer process ended. ");

                    // Collect info of the system and send them to the remote machine
                    Console.WriteLine("Collecting report...");
                    string reportPath = PrepareReport(proc, DEFAULT_REPORT_PATH);
                    Console.WriteLine("Sending report to HostController...");
                    ReportWork(j,reportPath,"completed");
                    Console.WriteLine("Done.");

                    // Done!
                    Console.WriteLine("Job completed!");

                    // We finished the test successfully. 
                    // In case this was a bare-metal VM, we need to reboot now. 
                    // In case of a VM, the situation may be different. For performance
                    // reasons it would be nice to start again from a known snapshot
                    // simply reverting the VM instead of full reboot. 
                    // At the moment we just reboot ourself, but in future this operation
                    // may be different.
                    NativeMethods.Reboot();
                    keepRunning = false;
                }
            }
            catch (Exception e)
            {
                // TODO: if the error happened during execution of interaction whould we notify the server that we failed? 
                // TOD: Notify the server!

                //MessageBox.Show(e.Message + "\n" + e.StackTrace);
                keepRunning = false;
                Console.Error.WriteLine("Exception " + e.Message + " occurred.");
                //TODO shall we reboot?! For now I raise the same error
                throw e; // FIXME!
            }
        }

        private string PrepareReport(ProcessContainer p, string outfile)
        {
            // Start collecting info and produce a nice report
            string errors = p.Process.StandardError.ReadToEnd();
            string output = p.Process.StandardOutput.ReadToEnd();
            int rtnCode = p.Process.ExitCode;

            // Add the output/error streams and return code
            var log = Program.GetInstallerLog();
            var executionResult = log.CreateElement("ExecutionResult");
            
            var stdout = log.CreateElement("StdOut");
            stdout.InnerText = output;
            executionResult.AppendChild(stdout);
            
            var stderr = log.CreateElement("StdErr");
            stderr.InnerText = errors;
            executionResult.AppendChild(stderr);

            var retcode = log.CreateElement("retcode");
            retcode.InnerText = "" + rtnCode;
            executionResult.AppendChild(retcode);

            log.FirstChild.AppendChild(executionResult);

            // Write the collected info to a local report.xml file.
            using (var fs = File.Create(outfile))
            {
                using (XmlWriter xmlWriter = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    log.WriteTo(xmlWriter);

                    // Flush changes on disk.
                    fs.Flush();
                }
            }

            return outfile;

        }

        private string GetMACAddr() {

            string macAddr =
                (
                    from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                    select nic.GetPhysicalAddress().ToString()
                ).FirstOrDefault();

            // Add separators to the mac
            StringBuilder builder = new StringBuilder();
            for (int i=0;i<macAddr.Length;i++) {
                if ((i % 2) == 0 && i != 0)
                    builder.Append(':');
                builder.Append(macAddr[i]);
            }

            return builder.ToString();
        
        }

        private Window WaitForInputRequested(ProcessContainer pc)
        {
            // There's no progragmatic way to understand that, so I'll apply a sort of logic strategy
            /**
             * According to me, an UI is waiting for the user input if:
             * 1. There are clickable controls on the focused window
             * 2. The process is not heavy-working on the background/foreground (monitor its CPU consuption, as well as IO and so on)
             * 3. The UI is not changing
             * If all of those conditions are the same for more than 2 seconds (i.e. 4 iterations) I guess the UI
             * is waiting for the user input.
             */
            int pollInterval = 300;
            int ciclesLimit = 3;
            int ciclesDone=0;

            IntPtr prevHandle = IntPtr.Zero;
            Window actualWindow = null;
            Window prevWindow = null;

            while (ciclesDone <= ciclesLimit)
            {
                try
                {
                    // If No window is spawned, wait...
                    IntPtr wH = GetProcUIWindowHandle(pc.Process.Id);
                    Console.WriteLine("Window Handle: " + wH);
                    if (wH == IntPtr.Zero)
                    {
                        Console.WriteLine("Window Handle: NO WINDOW");
                        if (pc.Process.HasExited)
                        {
                            throw new ProcessExitedException();
                        }
                        //Console.WriteLine("WAITING FOR INPUT: Window handle returned 0");
                        prevHandle = wH;
                        ciclesDone = 0;
                        Thread.Sleep(pollInterval);
                        continue;
                    }

                    // If the Handle of the mainWindow changes, restart the loop
                    if (!wH.Equals(prevHandle))
                    {
                        //Console.WriteLine("WAITING FOR INPUT: Window handle changed from "+prevHandle+" to "+wH);
                        prevHandle = wH;
                        ciclesDone = 0;
                        Thread.Sleep(pollInterval);
                        continue;
                    }

                    actualWindow = new Window(wH);

                    if (prevWindow == null)
                        prevWindow = actualWindow;
                    else
                        if (!prevWindow.Equals(actualWindow))
                        {
                            // Something changed
                            //Console.WriteLine("Something changed into the window even if handles are the same.");
                            prevWindow = actualWindow;
                            ciclesDone = 0;
                            Thread.Sleep(pollInterval);
                            continue;
                        }

                    // If there are no controls to interact with, restart the loop
                    if (actualWindow.ControlCount == 0)
                    {
                        Console.WriteLine("--- NO CONTROL TO INTERACT WITH ---");
                        //Console.WriteLine("WAITING FOR INPUT: No controls to interact with.");
                        //Console.WriteLine("Winhandle: "+wH+" - "+actualWindow.Handle);
                        //Console.WriteLine("Wintitle: " + wH + " - " + actualWindow.Title);
                        ciclesDone = 0;
                        Thread.Sleep(pollInterval);
                        continue;
                    }

                    // Is there any progressbar? if yes, proceed only if it is 100% completed...
                    // TODO

                    // What about CPU utilization of the process???
                    // If I get here, nothing seems to be changed, so increment the counter and wait
                    Thread.Sleep(pollInterval);
                    ciclesDone++;
                    }
                    catch (Exception e)
                    {
                        if (e is ProcessExitedException)
                            throw e;
                        else
                        {
                            ciclesDone = 0;
                            continue;
                        }
                    }
                }
            
            //Console.WriteLine("User Input requested. ");
            return actualWindow;
        }

        private IntPtr GetProcUIWindowHandle(int pid)
        {
            List<int> mypids = new List<int>();
            Process[] pcs = Process.GetProcesses();
            mypids.Add(pid);
            WalkProcessTree(pid, mypids, pcs);

            IntPtr desktophndl = GetDesktopWindow();

            Condition ctCon = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            Condition oC = Condition.FalseCondition;
            for (int i = 0; i < mypids.Count; i++)
            {
                oC=new OrCondition(oC,new PropertyCondition(AutomationElement.ProcessIdProperty,mypids.ElementAt(i))); // Tricky
            }
            
            Condition c = new AndCondition(ctCon, oC);

            AutomationElement ae = AutomationElement.FromHandle(desktophndl).FindFirst(TreeScope.Children,c);
            if (ae != null)
            {
                IntPtr res = new IntPtr(ae.Current.NativeWindowHandle);
                //Console.WriteLine("Current window Handle is: " + res);
                //Console.WriteLine("Class is: " + Window.GetClassName(res));
                //Console.WriteLine("Name is " + Window.GetWindowName(res));
                return res;
            }
            else
            {
                //Console.WriteLine("No windows found.");
                return IntPtr.Zero;
            }

            
            /*
            // Get the FocusedWindow and be sure it's owned by one of the current Processes
            IntPtr res = GetForegroundWindow();

            if (res != IntPtr.Zero)
            {
                // If there's a focused window, BE SURE it's owned by the spawn process.
                // Retrive the process ID of the current FOCUSED WINDOW
                uint p;
                GetWindowThreadProcessId(res, out p);
                // Scan all the processes
                foreach (int i in mypids)
                {
                    if (i == p)
                        return res;
                }

                Console.WriteLine("Current window Handle is: "+res);
                Console.WriteLine("Class is: " + Window.GetClassName(res));
                Console.WriteLine("Name is " + Window.GetWindowName(res));

                return IntPtr.Zero;
                
            }
            else
                // If There's no window on foreground, return a null POINTER
                return IntPtr.Zero;
            */
        }

        private void WalkProcessTree(int pid, List<int> pids, Process[] pcs)
        {
            foreach (Process p in pcs)
            {
                Process proc = null;
                try
                {
                    proc = ParentProcessUtilities.GetParentProcess(p.Id);
                    if (proc==null)
                        continue;
                }
                catch (Exception ex)
                {
                    continue;
                }

                int pp = proc.Id;
                if ( pp== pid)
                {
                    WalkProcessTree(p.Id, pids, pcs);
                    pids.Add(p.Id);
                }
            }
        }

        private ProcessContainer StartProcessWithInjector(Job j)
        {
            ProcessContainer res = null;
            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = Properties.Settings.Default.INJECTOR_PATH;
                proc.StartInfo.Arguments = "\"" + j.LocalFullPath + "\" " + "\"" + Properties.Settings.Default.DLL_PATH + "\" " + "\"" + Program.GetMainWindowName() + "\"";
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                res = new ProcessContainer(proc);
                res.Start();
                
            } catch(Exception ex){
                // Partially handle here. Kill the process if created and dispose the process object. Then raise the exception again.
                if (res != null)
                {
                    try { res.Process.Kill(); }
                    catch (Exception e) { /*Do nothing.*/ }
                    res.Process.Close();
                }
                throw ex;
            }
            
            return res;
        }

        #region Win32 API

        const int SWP_NOSIZE = 0x0001;

        private delegate bool EnumWindowsProc(IntPtr hWnd, ref int pid);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, ref int pid);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            NOSIZE = 0x0001
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        #endregion

    }


    /// <summary>
    /// A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParentProcessUtilities
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            Process process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }

}
