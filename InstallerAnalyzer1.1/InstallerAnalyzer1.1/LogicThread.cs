using InstallerAnalyzer1_Guest.Controls;
using System;
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

namespace InstallerAnalyzer1_Guest
{
    class LogicThread
    {
        private int _getWorkPollingTime = 5000; // Poll every 5 secs
        
        private string _installerPath;
        private Process _proc;
        private ProcessContainer _procJob;

        // Network objects
        private IPAddress _remoteIp;
        private int _remotePort;
        private TcpClient _tcpEndpoint;
        private BinaryWriter _nW;
        private BinaryReader _nR;
        private string _vmName;

        public LogicThread(IPAddress remoteIp, int remotePort)
        {
            _remoteIp = remoteIp;
            _remotePort = remotePort;
            _vmName = GetVMName();
            // Network setup: this will also connect to the remote host
        }

        public void Start()
        {
            SetupNetwork();
            Thread t = new Thread(new ThreadStart(work));
            t.Start();
        }

        private void work()
        {
            try
            {
                Console.WriteLine("Sending Call for work");
                // Ask for work
                SendCallForWork();
                // Wait until a positive answer arrives...
                while (!ServerHasWork())
                {
                    Console.WriteLine("The remote server hasn't work for me. I sleep...");
                    Thread.Sleep(_getWorkPollingTime);
                    SendCallForWork();
                }

                Console.WriteLine("Getting work from remote server...");
                // Retrive the work from the remote machine: ends with the ACK meaning File has been correctly read
                GetWork();

                Program.setSockAddr(((IPEndPoint)_tcpEndpoint.Client.LocalEndPoint).Address.ToString() + ":" + ((IPEndPoint)_tcpEndpoint.Client.LocalEndPoint).Port);
                Console.WriteLine("Starting installer...");
                // Start the process: ends with an ACK meaning the process has been correctly started
                StartProcess();
                SendAck();
                //Console.WriteLine("Sending ACK, process started correctly");
                Console.WriteLine("Installer started.");

                // Continue until the process exits.
                while (!IsProcessEnded())
                {
                    Console.WriteLine("Installer process still running...");
                    Window waitingWindnow = null;
                    // Wait for inputrequested
                    try
                    {
                        Console.WriteLine("Waiting for windows to be stable...");
                        waitingWindnow = WaitForInputRequested();
                        // Send PROCESS_GOING command
                        Console.WriteLine("Ok, ready for user input. Sending Process still running...");
                        SendProcessGoing();
                    }
                    catch (ProcessExitedException e)
                    {
                        break;
                    }

                    Console.WriteLine("Sending possible UI interactions -- ...");
                    // Now inform the remote machine about possible interactions
                    SendPossibleInteractions(waitingWindnow);
                    Console.WriteLine("Waiting for a command to perform from the remote server...");
                    // Wait for the remote decision
                    Int16 cmd = ReceiveCommand();
                    Console.WriteLine("Command received from remote server");
                    // Now execute command
                    try
                    {
                        Console.WriteLine("Executing command...");
                        ExecuteCommand(cmd, waitingWindnow);
                        Console.WriteLine("Command executed correctly.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception happened.");
                        using (StringWriter sw = new StringWriter())
                        {
                            // Build the XML Error element
                            using (XmlTextWriter xtw = new XmlTextWriter(sw))
                            {
                                xtw.WriteStartElement("ERROR");
                                xtw.WriteAttributeString("ExceptionMessage", e.Message);
                                xtw.WriteAttributeString("ExceptionStackTrace", e.StackTrace);
                                xtw.WriteEndElement();
                                xtw.Flush();
                            }
                            sw.Flush();
                            // Append that element to the log
                            Console.WriteLine(sw.ToString());
                            // Break the current while and send info to the host
                            break;
                        }
                    }

                }

                Console.WriteLine("Installer process ended. Sending info to remote server");
                SendProcessEnded();
                
                // Collect info of the system and send them to the remote machine
                Console.WriteLine("Sending info to the remote server...");
                SendInfoToMachine();
                Console.WriteLine("Done.");

                // Reboot
                //Console.Write("Remote has asked for reboot.");
                // Check if I am a virtual machine. If yes, ask the HOST to reboot me.
                if (IsVM())
                {
                    Console.WriteLine("I am a VM, ask the remote host to revert me.");
                    RemoteReboot();
                    Console.WriteLine("Done");
                }
                else
                {
                    Console.WriteLine("I am a physical VM, notify the remote server and reboot.");
                    // Tell Remote I'm a physical Machine
                    Console.WriteLine("Done. Rebooting...");
                    SendLocalRebootACK(); // This will drop connection.
                    LocalReboot();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n" + e.StackTrace);
            }
        }

        private void SendInfoToMachine()
        {
            // Is everything ok?
            string errors = _proc.StandardError.ReadToEnd();
            int rtnCode = _proc.ExitCode;

            // 1. Send the exit code
            _nW.Write(rtnCode);

            // 2. Send STD ERROR
            _nW.Write(errors);

            // 3. Send the whole log
            using (StringWriter tw = new StringWriter())
            {
                using (XmlWriter xmlWriter = new XmlTextWriter(tw))
                {
                    try
                    {
                        Program.GetInstallerLog().WriteTo(xmlWriter);
                        xmlWriter.Flush();
                        _nW.Write(tw.GetStringBuilder().ToString());
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("NO WAY!!!");
                    }
                }
            }
        }

        /*
        private static void SetupHeader()
        {
            IntPtr consolehandle = GetConsoleWindow();
            Screen s = Screen.PrimaryScreen;
            WINDOWPLACEMENT place = new WINDOWPLACEMENT();
            GetWindowPlacement(consolehandle, ref place);

            SetWindowPos(consolehandle, IntPtr.Zero, s.Bounds.Width - place.rcNormalPosition.Width, 0, 0, 0, SetWindowPosFlags.NOSIZE);
            Console.WindowHeight = Console.LargestWindowHeight;
            
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            //******************************************
            Console.CursorTop = 0; Console.CursorLeft = 0;
            Console.Write("******************************************");
            // Server Address: 
            Console.CursorTop = 1; Console.CursorLeft = 0;
            Console.Write(" Server Address: ");
            // Installer:
            Console.CursorTop = 2; Console.CursorLeft = 0;
            Console.Write(" Installer: ");
            // Process spawned:
            Console.CursorTop = 3; Console.CursorLeft = 0;
            Console.Write(" Process spawned: ");
            // Last Protocol Command:
            Console.CursorTop = 4; Console.CursorLeft = 0;
            Console.Write(" Last Protocol Command: ");
            //******************************************
            Console.CursorTop = 5; Console.CursorLeft = 0;
            Console.Write("******************************************");

            Console.CursorTop = 6;
            Console.CursorLeft = 0;

            Console.ResetColor();
        }
        */
        private void SendCallForWork()
        {
            _nW.Write(Protocol.JOB_POLL_MSG);
            //Console.WriteLine("I asked for work...");
            _nW.Write(_vmName);
        }

        private bool ServerHasWork()
        {
            // Receive feedback
            Int16 wr = _nR.ReadInt16();
            if (wr == Protocol.JOB_READY)
            {
                return true;
            }
            else if (wr == Protocol.NO_JOBS)
            {
                return false;
            }
            else
            {
                throw new ProtocolException("Invalid answer from host.");
            }
        }

        private bool IsVM()
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = "/c \"C:\\Program Files\\Oracle\\VirtualBox Guest Additions\\VBoxControl.exe\" guestproperty get VMNAME";
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();

            return p.ExitCode==0;
        }

        private void RemoteReboot()
        {
            // 1. Send RemoteRebootACK
            _nW.Write(Protocol.ACK_REMOTE_REBOOT);
            // 2. Send VMName
            _nW.Write(_vmName);
            // I won't close anything because an Hard Reboot is happening...
            //TODO: after a while local reboot if nothing happened.
        }

        private string GetVMName()
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = "/c \""+Settings.Default.GuestAdditionPath+"\" guestproperty get VMNAME";
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();

            string output = p.StandardOutput.ReadToEnd();

            if (!output.Contains("Value: "))
            {
                Console.WriteLine("OUTPUT: \""+output+"\"");
                throw new ApplicationException("Error: I am unable to find the VMName!");
            }
            else
            {
                string val = output.Split(new string[] {"Value: "},StringSplitOptions.None)[1];
                val = val.Replace("\"", "");
                val = val.Trim();
                return val;
            }


        }

        private void SendLocalRebootACK()
        {
            _nW.Write(Protocol.ACK_LOCAL_REBOOT);
            _nW.Close();
            _nR.Close();
            _tcpEndpoint.Close();
        }

        private void LocalReboot()
        {
            Process p = new Process();
            p.StartInfo.FileName = "shutdown";
            p.StartInfo.Arguments = " -r -f -t 0";
            p.Start();
            p.WaitForExit();

            //throw new Exception("NO REBOOT NOW!");
        }
        
        private void SendProcessGoing()
        {
            _nW.Write(Protocol.PROCESS_GOING);
        }

        private void SendProcessEnded()
        {
            _nW.Write(Protocol.PROCESS_ENDED);
        }

        private void SendCmdAck()
        {
            _nW.Write(Protocol.CMD_OK_ACK);
        }

        private void SendCmdNoAck()
        {
            _nW.Write(Protocol.CMD_NO_ACK);
        }

        private void SetupNetwork()
        {
            // This will also connect.
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
                    Console.WriteLine(String.Format("The remote node <{0}:{1}> seems to be down. Waiting...",_remoteIp,_remotePort));
                    Thread.Sleep(5000);
                }
            }
            //Console.WriteLine("Connected to " + _remoteIp + " : " + _remotePort);
            NetworkStream ns = _tcpEndpoint.GetStream();
            _nW = new BinaryWriter(ns);
            _nR = new BinaryReader(ns);
        }

        private void GetWork()
        {
            // Retrive the installer
            // 1. Read file name
            string fileName = _nR.ReadString();
            //Console.WriteLine("FileName received: " + fileName);

            // 2. Read File dimension
            long fileDim = _nR.ReadInt64();
            //Console.WriteLine("File Dimension received:" + fileDim);

            // Create the temporary file
            FileStream fs = File.Create(System.IO.Path.GetTempPath() + fileName);
            //Console.WriteLine("Created temp file in " + fs.Name);
            // TODO: System checks for space availability and permissions. If something is wrong, do not send ACK.

            // 3. Send ACK: I will accept that file. 
            SendAck();
            //Console.WriteLine("SENT ACK #1: OK, WAITING FOR FILE BINARIES.");

            // 4. Read the file from the network and save it locally
            Byte[] buff = new Byte[4096];
            int read = 0;
            while (read < fileDim) // Until you get the whole file...
            {
                // Read into the buffer from the network
                int nR = _nR.Read(buff, 0, buff.Length);
                if (nR == 0)
                    throw new NetworkProtocol.ProtocolException("I didn't receive all the file data, the remote host has closed the connection.");
                read += nR;
                // Write to the FileSystem
                fs.Write(buff, 0, nR);
            }
            _installerPath = fs.Name;
            fs.Close();
            fs.Dispose();

            // 5. Send ACK, File has been correctly READ.
            SendAck();
            //Console.WriteLine("SENT ACK #2: File received succesfully.");
        }

        private void ExecuteCommand(short cmd, Window w)
        {            
            // Parse the command and execute the asked action
            switch (cmd)
            {
                case Protocol.CMD_UI_INTERACT:
                    //Console.WriteLine("COMMAND TO PERFORM: UI INTERACTION");
                    // 1. Read the command
                    string id = _nR.ReadString();
                    // 1. Read the interaction type
                    int intType = _nR.ReadInt32();

                    string controlTitle = "NA";
                    foreach(InteractiveControl ic in w.InteractiveControls)
                    {
                        if (ic.Id == id)
                            controlTitle = ic.Text;
                    }

                    // Do the action
                    bool b = DoUiInteraction(id, intType, w);
                    // Send Ack
                    if (b)
                    {
                        SendCmdAck();
                        Program.appendFollowedPath(id + "=" + intType,"\'"+controlTitle+"\' ("+intType+")");
                    }
                    else
                    {
                        Console.WriteLine("An error has occurred during interaction with:");
                        Console.WriteLine("Control ID: " + id);
                        Console.WriteLine("Interaction type: " + intType);
                        SendCmdNoAck();
                    }

                    break;
                
                case Protocol.CMD_REMOTE_REBOOT:
                    if (IsVM())
                    {
                        RemoteReboot();
                    }
                    else
                    {
                        // Tell Remote I'm a physical Machine
                        SendLocalRebootACK(); // This will drop connection.
                        LocalReboot();
                    }
                    break;

                case Protocol.CMD_RESTART_PROCESS:
                    //Console.WriteLine("COMMAND TO PERFORM: RESTART PROCESS");
                    RestartProcess();
                    SendProcessRestartedAck();
                    break;
                default:
                    throw new ProtocolException("Invalid command specified: " + cmd);
            }    
        }

        private void RestartProcess()
        {
            Program.resetLog();
            _proc.Kill();
            _procJob.Close();
            _procJob.Dispose();
            _procJob = new ProcessContainer();
            _proc.Start();
            _procJob.AddProcess(_proc.Handle);
        }

        private void SendProcessRestartedAck()
        {
            _nW.Write(Protocol.ACK_PROCESS_RESTARTED);
        }

        private bool DoUiInteraction(string id, int intType, Window w)
        {
            foreach (InteractiveControl ic in w.InteractiveControls)
            {
                if (ic.Id.Equals(id))
                {
                    try
                    {
                        ic.Interact(intType);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        Console.WriteLine("I was unable to interact with "+ic);
                        return false;
                    }
                    
                }
            }
            return false;
        }

        private void SendAck()
        {
            _nW.Write(Protocol.ACK);
        }

        private Int16 ReceiveCommand()
        {
            return _nR.ReadInt16();
        }

        private Window WaitForInputRequested()
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
                    IntPtr wH = GetProcUIWindowHandle();
                    Console.WriteLine("Window Handle: " + wH);
                    if (wH == IntPtr.Zero)
                    {
                        Console.WriteLine("Window Handle: NO WINDOW");
                        if (_proc.HasExited)
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

                    // If the window is Windows Internet Explorer or notepad, close them directly
                    if (actualWindow.Title.Contains("Notepad") || actualWindow.Title.Contains("Internet Explorer"))
                    //if (actualWindow.ClassName.Equals("Notepad") || actualWindow.ClassName.Equals("Internet Explorer"))
                    {
                        actualWindow.Close();
                        continue;
                    }

                    // If there are no controls to interact with, restart the loop
                    if (actualWindow.InteractiveControls.Count() == 0)
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

        /*
        static int pc = 0;
        static readonly string[] wps = new string[] { "   ",".  ",".. ","..."};
        private static void UpdateHeader()
        {
            // Save the cursor pos
            int px = Console.CursorLeft;
            int py = Console.CursorTop;

            // Server Address: 
            Console.CursorTop = 1; Console.CursorLeft = 17;
            Console.Write(((IPEndPoint)_tcpEndpoint.Client.RemoteEndPoint).Address + " : " + ((IPEndPoint)_tcpEndpoint.Client.RemoteEndPoint).Port+" - ");
            if (_tcpEndpoint.Connected)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("CONNECTED");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("DISCONNECTED");
                Console.ResetColor();
            }

            // Installer:
            Console.CursorTop = 2; Console.CursorLeft = 12;
            if (_installerPath.Length>20)
                Console.Write("..."+_installerPath.Substring(_installerPath.Length-20));
            else
                Console.Write(_installerPath);
            // Process spawned:
            Console.CursorTop = 3; Console.CursorLeft = 18;
            string line = _proc.Id + " (" + (_proc.HasExited ? (" Exited as " + _proc.ExitCode) : (" Running"))+ wps[pc] + " )";
            if (pc == 3)
                pc = 0;
            else
                pc++;

            Console.WriteLine(line);
            
            Console.CursorLeft = px;
            Console.CursorTop = py;
        }
        */

        private IntPtr GetProcUIWindowHandle()
        {
            // First, get ALL PIDS spawned by the primary process: I have to monitor them all.
            int pid = _proc.Id;

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

        private bool IsProcessEnded()
        {
            // Check for all the PIDs related to the PROC object
            return _proc.HasExited;
        }

        /// <summary>
        /// The following function will start the received process using an appropriate injector.
        /// </summary>
        private void StartProcess()
        {
            if (_proc != null || _procJob != null)
                throw new ApplicationException("You cannot start that process twice!");
            
            _procJob = new ProcessContainer();
            _proc = new Process();
            _proc.StartInfo.FileName = Properties.Settings.Default.INJECTOR_PATH;
            _proc.StartInfo.Arguments = "\"" + _installerPath + "\" " + "\"" + Properties.Settings.Default.DLL_PATH + "\" " + "\"" + Program.GetMainWindowName()+"\"";
            _proc.StartInfo.RedirectStandardError = true;
            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.UseShellExecute = false;
            _proc.Start();
            _procJob.AddProcess(_proc.Handle);
            
            // TODO: insert a sync point. For instance wait until the first window of the installer appears by reading something from STDIN...
        }

        private void SendPossibleInteractions(Window w)
        { 
            // 1. Send window handle
            Console.WriteLine("Sending window handle to the remote server.");
            _nW.Write(w.Handle.ToInt32());
            // 2. Send number of interactive controls
            int count = w.InteractiveControls.Count();
            Console.WriteLine("Sending "+count+" controls to the remote server.");
            _nW.Write(count);
            // 3. For each InteractiveControl, send ID, Type
            int num = 0;
            foreach (InteractiveControl ic in w.InteractiveControls)
            {
                // Send button - Id
                _nW.Write(ic.Id);
                // Send Interaction id
                _nW.Write(ic.GetPossibleInteractions()[0].Id);
                // Send info about that button: text, bounds, focus status, (color)
                // Text
                _nW.Write(ic.Text);
                // Bounds: calculate ic bounds relative to the window
                Rect wB = w.GetBounds();
                Rect cB = ic.GetBounds();
                _nW.Write(cB.TopLeft.X-wB.TopLeft.X); // Top left x
                _nW.Write(cB.TopLeft.Y - wB.TopLeft.Y); // Top left Y
                _nW.Write(cB.BottomRight.X-wB.TopLeft.X); // Bottom right X
                _nW.Write(cB.BottomRight.Y - wB.TopLeft.Y);  // Bottom right Y
                // Focused?
                _nW.Write(ic.IsFocused());

                Console.WriteLine("Correctly sent control "+num);
                num++;
            }
            // 4. Send WindowStatusHash
            _nW.Write(w.StatusHash);
            // 5. Send Window screenshot: dimension and bytes
            byte[] buff = w.GetWindowsScreenshot();
            _nW.Write(buff.Length);
            _nW.Write(buff);
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
