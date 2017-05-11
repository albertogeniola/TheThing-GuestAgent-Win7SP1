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
using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Xml;
using InstallerAnalyzer1_Guest.Properties;
using System.Net.NetworkInformation;
using InstallerAnalyzer1_Guest.Protocol;
using Newtonsoft.Json;
using InstallerAnalyzer1_Guest.UIAnalysis;
using InstallerAnalyzer1_Guest.UIAnalysis.RankingPolicy;
using System.Drawing;
using Ionic.Zip;
using System.Xml.Serialization;

namespace InstallerAnalyzer1_Guest
{
    class LogicThread
    {
        const int STUCK_THRESHOLD = 20; //TODO: Fixme. This might be increased or decreased. 
        const int STUCK_NO_CONTROLS_THRESHOLD = 100;
        const int REACTION_TIMEOUT = 500;
        int IDLE_TIMEOUT = Settings.Default.IDLE_TIMEOUT; // Wait up to 10 minutes for heavy I/O timeout
        const int ACQUIRE_WORK_SLEEP_SECS = 10;
        const int CIRCULAR_LOOP_THRESHOLD = 5;
        readonly string DEFAULT_REPORT_PATH = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "report.xml");

        const int LOG_PER_SEC_IDLE_THRESHOLD = 50;

        // I use a dictionary that will contain hashes about scanned windows.
        // This will be useful to prevent circular UI loopings.
        private Dictionary<string, int> _visitedVindows;

        // This list will contain the list of installed programs as shown in Control Panel
        private List<string> _originalList;

        // We will use a timer in order to stop if the process is taking too long.
        private System.Timers.Timer _interactionTimer;
        private System.Timers.Timer _stuckUiWatcher;

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
        private void __send(NetworkStream ns, byte[] data)
        {
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

            while (tot < dim)
            {
                int toread = ((dim - tot) > buff.Length) ? buff.Length : (dim - tot);
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
        private void _send_message(NetworkStream ns, string msg)
        {
            // Encode the string and get its byte length
            var utf8 = Encoding.UTF8;
            byte[] utfBytes = utf8.GetBytes(msg);

            // Convert byte length to network unsigned int
            int len = IPAddress.HostToNetworkOrder(utfBytes.Length);

            // now send the len
            __send(ns, BitConverter.GetBytes(len));

            // finally send binary data
            __send(ns, utfBytes);

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
            _send_message(ns, JsonConvert.SerializeObject(req));
            return JsonConvert.DeserializeObject<ResponseGetWork>(_recv_message(ns));
        }

        private ResponseReportWork SendReport(NetworkStream ns, long workId, long reportFileLen, string status)
        {
            RequestReportWork r = new RequestReportWork();
            r.Mac = _mac;
            r.ReportLenInBytes = reportFileLen;
            r.WorkId = workId;
            r.Status = status;

            var info = GetNetworkInfo();
            var default_gw = info.GetIPProperties().GatewayAddresses.FirstOrDefault().Address.ToString();
            var my_addr = info.GetIPProperties().UnicastAddresses.ToString();
            // We also need to specify our current network configuration.
            r.NetworkConf = new NetworkConf() {
                DefaultGw = default_gw,
                GuestIp = my_addr,
                HcIp = ProgramStatus.Instance.HostControllerAddr.Address.ToString(),
                HcPort = ProgramStatus.Instance.HostControllerPort
            };

            _send_message(ns, JsonConvert.SerializeObject(r));
            return JsonConvert.DeserializeObject<ResponseReportWork>(_recv_message(ns));
        }
        private ResponseReportWorkReportReceived WaitServerReportAck(NetworkStream ns)
        {
            return JsonConvert.DeserializeObject<ResponseReportWorkReportReceived>(_recv_message(ns));
        }
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
                using (var fs = File.OpenRead(reportFilePath))
                {
                    __sendFile(ns, fs, (int)f.Length);
                }

                // Did the server receive the file correctly?
                ResponseReportWorkReportReceived rr = WaitServerReportAck(ns);
                if (!rr.isValid(out err))
                    //TODO deal with NACK
                    throw new ProtocolException("Received response by Host Controller was not valid. " + err);

                // Done!
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occurred while reporting work: " + e.Message + ". Trace: " + e.StackTrace);
                throw e;
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

                // This means the server has nothing to do atm. Let the caller decide what to do.
                if (res.WorkId == null)
                {
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

                // File received. Let the HostController we are now waiting for START MESSAGE
                ResponseGetWorkStartAnalysis rr = new ResponseGetWorkStartAnalysis();
                _send_message(ns, JsonConvert.SerializeObject(rr));
                
                Console.WriteLine("File received. Waiting for start signal from server...");
                var clearance = JsonConvert.DeserializeObject<ResponseGetWorkStartAnalysis>(_recv_message(ns));

                Console.WriteLine("Ready to go!");
                Job j = new Job((long)res.WorkId, path);
                return j;
            }
            finally
            {
                client.Close();
            }
        }
        #endregion

        private bool AreProcsFinished()
        {
            foreach (uint p in ProgramStatus.Instance.Pids)
            {
                try
                {
                    Process proc = Process.GetProcessById((int)p);
                    if (!proc.HasExited)
                        return false;
                }
                catch (ArgumentException e)
                {
                    // The process is dead. Remove it from the list of monitored ones.
                    Console.WriteLine("AreProcsFinished: Process " + ((int)p) + " may be dead. Skipping.");
                    ProgramStatus.Instance.RemovePid(p);
                }
            }

            return true;
        }

        private bool _timeout;
        private int _c = 0;
        private ProcessContainer ExecuteJob(Job j, IUIRanker ranker, IRankingPolicy policy)
        {
            _c = 0;
            int stuckCounter = 0;
            int stuckWaitingForControlsCounter = 0;
            PrepareScreenFolders();

            // Start the process to analyze
            Console.WriteLine("UI Bot: START");
            var proc = StartProcessWithInjector(j);
            Console.WriteLine("UI Bot: Process started, pid " + proc.Process.Id);
            j.StartTime = DateTime.Now;

            // Wait 2 seconds and check if the injector failed. If so, 
            proc.Process.WaitForExit(2000);
            if (proc.Process.HasExited && proc.Process.ExitCode != 0)
            {
                Console.WriteLine("UI Bot: Process has died just after it was spawned. This is usually happens with non-executable files or unsupported files.");
                proc.Result = InteractionResult.UnknownError;
                return proc;
            }

            // Before entering the check, give some time to the UI to spawn
            Thread.Sleep(10000);

            // Keep interacting until we hit the timeout or the process exits normally.
            while (!_timeout && !ProgramStatus.Instance.MitmSucceded)
            {
                try
                {
                    // Check if there is still some process running. If no, break!
                    if (AreProcsFinished())
                        throw new ProcessExitedException();

                    // Wait until the window is considered stable
                    Window waitingWindnow = null;
                    Console.WriteLine("UI Bot: WAIT WINDOW STABLE (PID: " + proc.Process.Id + ") " + "Interaction: " + _c);
                    waitingWindnow = WaitForInputRequested();

                    if (waitingWindnow == null)
                    {
                        // No window has been found. Let the loop check again if there still are processes running, otherwise we are done!
                        Console.WriteLine("UI Bot: No windows found. Check again wether any process is running...");
                        if (ProgramStatus.Instance.Pids.Length == 0 && ProgramStatus.Instance.LogsPerSec == 0)
                        {
                            Console.WriteLine("UI Bot: All processes are ended and lograte is 0. It's done!");
                            break;
                        }
                        else
                        {
                            // TODO: we have to introduce a MAX counter here to detect when a process is stuck
                            // or working too much.
                            Console.WriteLine("UI Bot: However there still are " + ProgramStatus.Instance.Pids.Length + " processes working. Receiving " + ProgramStatus.Instance.LogsPerSec + " messages/sec by injected dll.");
                            continue;
                        }
                    }

                    // Otherwise we have a window on the foreground. 
                    Console.WriteLine("UI Bot: Stable hWND " + waitingWindnow.Handle.ToString("X") + ", loc: " + waitingWindnow.WindowLocation.ToString());

                    SaveStableScreen(waitingWindnow, _c);

                    // Analyze the window and build the controls rank.
                    Console.WriteLine("UI Bot: Analyze Window (PID: " + proc.Process.Id + ", HWND: " + waitingWindnow.Handle + ", TITLE: " + waitingWindnow.Title + ") " + "Interaction: " + _c);
                    CandidateSet w = ranker.Rank(policy, waitingWindnow);

                    // Make sure there is no scrollbar...
                    if (w.HasIncompleteProgressBar())
                    {
                        Console.WriteLine("UI Bot: Scrollbar Detected! Skipping...");
                        continue;
                    }

                    // Now let the interaction happen. The RankingPolicy decides which UIControl should we use to continue installation
                    bool uiHasChanged = false;
                    do
                    {
                        // Waiting if there is any high consuming operation running.
                        // Wait until we get 2 seconds stability or we hit 30 seconds timeout.
                        ProgramStatus.Instance.WaitUntilNotBusy(LOG_PER_SEC_IDLE_THRESHOLD, 2, 30);

                        // Interact with the best control according to the rank assinged by the Interaction Policy
                        var candidate = w.PopTopCandidate();

                        // We only accept positive scores. 
                        if (candidate.Score > 0)
                        {
                            Console.WriteLine("UI Bot: Interacting with control " + candidate.ToString());

                            // Register our intention to interact with a particular window
                            int counter;
                            if (_visitedVindows.TryGetValue(w.Hash, out counter))
                            {
                                _visitedVindows[w.Hash]++;
                            }
                            else
                            {
                                counter = 1;
                                _visitedVindows.Add(w.Hash, counter);
                            }

                            // Are we interacting again with the same window as before? Have we exceeded the threshold?
                            if (counter > CIRCULAR_LOOP_THRESHOLD)
                            {
                                throw new UILoopException();
                            }

                            // Save a screenshot with interaction information for debugging and reporting
                            SaveInteractionScreen(waitingWindnow, w, candidate, _c);

                            candidate.Interact();
                            _c++;

                            // Wait for something to happen
                            Console.WriteLine("UI Bot: Waiting for UI reaction.");
                            uiHasChanged = ranker.WaitReaction(waitingWindnow, w, REACTION_TIMEOUT);

                            // If we hit the timeout, check if there still are process running. If not, just break.
                            if (!uiHasChanged)
                            {
                                Console.WriteLine("UI Bot: UI Reaction didn't happen within the specific TIMEOUT.");
                                int procs = ProgramStatus.Instance.Pids.Length;
                                long lograte = ProgramStatus.Instance.LogsPerSec;
                                if (procs == 0 && lograte == 0)
                                {
                                    Console.WriteLine("UI Bot: All processes are ended and lograte is 0. It's done!");
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("UI Bot: but there still are " + procs + " processes running and lograte is " + lograte + " msg/sec. I keep waiting.");
                                }
                            }
                        }
                        else {
                            Console.WriteLine(String.Format("UI Bot: candidate {1} had negative score of {0}. Not interacting with it.", candidate.Score, candidate.ToString()));
                            // Simulate interaction even if we didn't interact
                            Thread.Sleep(3000);
                        }

                    } while (!uiHasChanged && !_timeout);

                }
                catch (ControlNotFoundException e)
                {
                    // The control we were looking for does not exist on the window
                    Console.WriteLine("UI Bot: the espected control was not found on the UI. The UI might have been refreshed. We'll analyze it again.");
                    // So we want to repeat the loop again to analyze it again
                    continue;
                }
                catch (ElementNotEnabledException e)
                {
                    Console.WriteLine("UI Bot: Element has become disabled.");
                    if (stuckCounter < STUCK_THRESHOLD)
                    {
                        stuckCounter++;
                        Console.WriteLine("UI Bot: Performing another attempt to find a valid window");
                        continue;
                    }
                    else
                    {
                        proc.Result = InteractionResult.UI_Stuck;
                        Console.WriteLine("UI Bot: maximum number of attempts reached. Givin' up.");
                        break;
                    }
                }
                catch (ElementNotAvailableException e)
                {
                    Console.WriteLine("UI Bot: Element disappeared");
                    if (stuckCounter < STUCK_THRESHOLD)
                    {
                        stuckCounter++;
                        Console.WriteLine("UI Bot: Performing another attempt to find a valid window");
                        continue;
                    }
                    else
                    {
                        proc.Result = InteractionResult.UI_Stuck;
                        Console.WriteLine("UI Bot: maximum number of attempts reached. Givin' up.");
                        break;
                    }
                }
                catch (NoMoreCandidateException e)
                {
                    // We are stuck and the interaction policy is unable to find
                    // a valid control to interact with. 
                    // Increase the stuck counter and try again. Maybe the window has changed.
                    // if the counter if abose the threshold, give up.
                    Console.WriteLine("UI Bot: No more control to interact with.");
                    if (stuckWaitingForControlsCounter < STUCK_NO_CONTROLS_THRESHOLD)
                    {
                        stuckWaitingForControlsCounter++; //TODO: No more candidate means a window with no interaction possibilities. 
                        // Should we consider it as Stuck? It may be a simple waiting window... This counter should be different and higer.
                        Console.WriteLine("UI Bot: Performing another attempt to find a valid window");
                        continue;
                    }
                    else
                    {
                        proc.Result = InteractionResult.UI_Stuck;
                        Console.WriteLine("UI Bot: maximum number of attempts reached. Givin' up.");
                        break;
                    }
                }
                catch (UILoopException e)
                {
                    // We ended up into a loop among same UI interfaces. This may happer for some reasons, like "Abort->Cancel->Abort->Cancel..."
                    // or id the UI requires some particular interacion we can't emulate. We can choose several approaches to this situation:
                    // -> Simply report failure: our UI interaction policy was not able to finish the installation process
                    // -> Wait until the processes on the background are working and re-evaluate then the situation
                    // -> Check if there is something new as "Installed Features" into the windows registry, so we know that the installation process went partially ok.
                    Console.WriteLine("UIBot: UI Loop Detected.");
                    var pids = ProgramStatus.Instance.Pids.Length;
                    var lograte = ProgramStatus.Instance.LogsPerSec;
                    Console.WriteLine("UIBot: There are still " + pids + " running, transmitting " + lograte + " msg/s.");

                    var delta = CheckNewPrograms();
                    if (delta.Count > 0)
                    {
                        Console.WriteLine("UIBot: There are new programs on the system. Assuming the installation was successful.");
                        proc.Result = InteractionResult.PartiallyFinished;
                        // Something happened on the system! We can assume the installation process had some effect.
                        break;
                    }

                    Console.WriteLine("UIBot: There are no new programs on the system.");

                    if (ProgramStatus.Instance.LogsPerSec > LOG_PER_SEC_IDLE_THRESHOLD)
                    {
                        Console.WriteLine("UIBot: The background processes are still working. Wait until the message rate becomes <50 (or timeout is reached) and loop again.");
                        if (!ProgramStatus.Instance.WaitUntilIdle(IDLE_TIMEOUT))
                        {
                            // Timeout occurred
                            Console.WriteLine("UIBot: This delay is taking too much. Giving up reporting failure.");
                            proc.Result = InteractionResult.UI_Stuck;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("UIBot: UI is idle now. I will run the analysis again.");
                            // I need to clear the status of the windows otherwise we are going to fall again under this catch.
                            _visitedVindows.Clear();
                            continue;
                        }

                    }

                    // Report failure. We were unable to recover from this loop.
                    Console.WriteLine("UIBot: There is nothing I can do to recover. Process seem to wait an action I am unable to perform.");
                    proc.Result = InteractionResult.UI_Stuck;
                    break;
                }
                catch (ProcessExitedException e)
                {
                    // The process exited. There is nothing we can do to recover.
                    Console.WriteLine("UI Bot: Process exited while interacting with UI.");
                    proc.Result = InteractionResult.Finished;
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("UI Bot: exception occurred. " + e.Message + ". " + e.StackTrace);
                    // Just for debugging reasons, throw it again
                    proc.Result = InteractionResult.UnknownError;
                    break;
                }

                // At this point we reiterate again, until the pocess runs.
            } // End of the interaction Loop


            // The control gets here when the previous interaction loop is finished. It may finish for the following reasons:
            // -> All the processes end correctly: status will be Finished
            // -> Detected UI Stuck: UIStuck
            // -> An unhandled error occurs: Unknown
            // However timeout check is performed within the while evaluation, so we need to check it separately and 
            // act accordingly. If the timeout was hit, we have to kill everything and return the control to the caller.

            // Save one entire screenshot
            using (Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                            Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                     Screen.PrimaryScreen.Bounds.Y,
                                     0, 0,
                                     bmpScreenCapture.Size,
                                     CopyPixelOperation.SourceCopy);
                }
                bmpScreenCapture.Save(Path.Combine(Settings.Default.INTERACTIONS_SCREEN_PATH, _c + ".bmp"));
            }

            // Make sure everything is terminated before continuing
            if (!proc.Process.HasExited)
                proc.Process.Kill();

            // Check if we got a timeout
            if (_timeout)
            {
                proc.Result = InteractionResult.TimeOut;
            }
            
            j.EndTime = DateTime.Now;

            // Return the result to the parent.
            return proc;
        }

        private void PrepareScreenFolders()
        {
            if (!Directory.Exists(Settings.Default.INTERACTIONS_SCREEN_PATH))
                Directory.CreateDirectory(Settings.Default.INTERACTIONS_SCREEN_PATH);
            else
                foreach (FileInfo file in (new DirectoryInfo(Settings.Default.INTERACTIONS_SCREEN_PATH)).GetFiles())
                {
                    file.Delete();
                }

            if (!Directory.Exists(Settings.Default.STABLE_SCREEN_PATH))
                Directory.CreateDirectory(Settings.Default.STABLE_SCREEN_PATH);
            else
                foreach (FileInfo file in (new DirectoryInfo(Settings.Default.STABLE_SCREEN_PATH)).GetFiles())
                {
                    file.Delete();
                }
        }

        private bool SaveStableScreen(Window waitingWindnow, int c)
        {
            try
            {
                using (Bitmap b = waitingWindnow.GetWindowsScreenshot())
                {
                    if (b == null)
                        return false;
                    string fname = Path.Combine(Settings.Default.STABLE_SCREEN_PATH, c + "_" + "{0}" + ".bmp");
                    string hash = UIAnalysis.NativeAndVisualRanker.CalculateHash(b, fname);
                    fname = string.Format(fname, hash);
                    b.Save(fname);
                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }


        private static readonly Font scoreFont = new Font(FontFamily.GenericSansSerif, 10.0F, FontStyle.Bold);
        private static readonly Brush whiteBrush = new SolidBrush(Color.White);
        private static readonly Brush secondChoiceScoreBrush = new SolidBrush(Color.Blue);
        private static readonly Pen secondChoicePen = new Pen(Color.Blue, 3);
        private static readonly Pen markerPen = new Pen(Color.Red, 5);
        private static readonly Brush firstChoiceScoreBrush = new SolidBrush(Color.Red);
        private bool SaveInteractionScreen(Window waitingWindnow, CandidateSet w, UIControlCandidate best, int c)
        {
            try
            {
                using (Bitmap b = waitingWindnow.GetWindowsScreenshot())
                {
                    if (b == null)
                        return false;

                    // Save the clean image first
                    string fname = Path.Combine(Settings.Default.INTERACTIONS_SCREEN_PATH, c + ".bmp");
                    string clean_fname = Path.Combine(Settings.Default.INTERACTIONS_SCREEN_PATH, "clean_" + c + ".bmp");
                    string windo_details_fname = Path.Combine(Settings.Default.INTERACTIONS_SCREEN_PATH, c + ".xml");

                    // Save the clean image first
                    b.Save(clean_fname);

                    // Save info about the current score set.
                    using (var xmlWriter = XmlWriter.Create(windo_details_fname))
                    {
                        best.IsBest = true;
                        w.Add(best);
                        // Serialize the object, and close the TextWriter.
                        XmlSerializer s = new XmlSerializer(typeof(CandidateSet));
                        s.Serialize(xmlWriter, w);
                        w.Remove(best);
                    }

                    // Now draw overlays on the image. Useful for fast scan
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        string text = "";
                        var size = new SizeF();
                        var rect = new RectangleF();
                        // Draw the candidate set
                        foreach (var item in w)
                        {
                            text = "" + item.Score;
                            g.DrawRectangle(secondChoicePen, item.PositionWindowRelative);
                            size = g.MeasureString(text, scoreFont);
                            rect = new RectangleF(item.PositionWindowRelative.X, item.PositionWindowRelative.Y - scoreFont.Height, size.Width, size.Height);
                            g.FillRectangle(whiteBrush, rect);
                            g.DrawString(text, scoreFont, secondChoiceScoreBrush, item.PositionWindowRelative.X, item.PositionWindowRelative.Y - scoreFont.Height);
                        }

                        // Darw the selected one
                        g.DrawRectangle(markerPen, best.PositionWindowRelative);
                        text = "" + best.Score;
                        g.DrawRectangle(secondChoicePen, best.PositionWindowRelative);
                        size = g.MeasureString(text, scoreFont);
                        rect = new RectangleF(best.PositionWindowRelative.X, best.PositionWindowRelative.Y - scoreFont.Height, size.Width, size.Height);
                        g.FillRectangle(whiteBrush, rect);
                        g.DrawString(text, scoreFont, firstChoiceScoreBrush, best.PositionWindowRelative.X, best.PositionWindowRelative.Y - scoreFont.Height);

                        b.Save(fname);
                        return true;

                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public LogicThread(IPAddress remoteIp, int remotePort)
        {
            _remoteIp = remoteIp;
            _remotePort = remotePort;
            _mac = GetMACAddr();
            _visitedVindows = new Dictionary<string, int>();
            _originalList = new List<string>();
            _interactionTimer = new System.Timers.Timer(Settings.Default.EXECUTE_JOB_TIMEOUT);
            _stuckUiWatcher = new System.Timers.Timer(60000);
            _stuckUiWatcher.Elapsed += _stuckUiWatcher_Elapsed;
        }

        void _stuckUiWatcher_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Check if there is any stuck window. If so, kill the owning process
            var pids = ProgramStatus.Instance.Pids;
            foreach (var p in pids)
            {
                try
                {
                    Process proc = Process.GetProcessById((int)p);
                    UIntPtr r;
                    IntPtr res = NativeMethods.SendMessageTimeout(proc.MainWindowHandle, (uint)0, UIntPtr.Zero, IntPtr.Zero, NativeMethods.SendMessageTimeoutFlags.SMTO_ABORTIFHUNG | NativeMethods.SendMessageTimeoutFlags.SMTO_BLOCK, 5000, out r);
                    if (res == IntPtr.Zero)
                    {
                        int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                        if (err == 1460L)
                        {
                            // The window is stuck. Kill the owning process and proceed.
                            Console.WriteLine("UI Watcher: Detected some UI stuck. Killing owning process " + proc.Id);
                            proc.Kill();
                        }
                    }

                }
                catch (Exception ex)
                {
                    // Ignore everything. This is not crucial.
                }
            }
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
            IUIRanker ranker = new NativeAndVisualRanker();
            IRankingPolicy policy = new SimpleRankingPolicy();

            // Take a snapshot of currently installed programs
            _originalList = Program.ListInstalledPrograms();

            bool keepRunning = true;

            while (keepRunning)
            {
                Job j = null;
                ProcessContainer proc = null;

                // Acquire the job from the server.
                // This may return null in case the server has nothing to do.
                try
                {
                    j = AcquireWork();
                }
                catch (Exception e)
                {
                    // Keep trying. The server mght be down.
                    Console.WriteLine("Error when acquiring Job from server. " + e.Message + ";\n" + e.StackTrace);
                    Thread.Sleep(ACQUIRE_WORK_SLEEP_SECS * 1000);
                    continue;
                }

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
                try
                {
                    // Start a timeout in order to recover if the process gets stuck
                    _interactionTimer.Elapsed += TimeoutReached;
                    _timeout = false;

                    _interactionTimer.Start();
                    _stuckUiWatcher.Start();
                    proc = ExecuteJob(j, ranker, policy);
                    _interactionTimer.Stop();
                    _stuckUiWatcher.Stop();

                    Console.WriteLine("Installer process ended. ");
                }
                catch (Exception e)
                {
                    // I don't know what did it happen. Just report failure.
                    proc.Result = InteractionResult.UnknownError;
                    Console.WriteLine("Exception occurred when ExecutingJob: " + e.Message + "\n" + e.StackTrace);
                }

                // Stop listening for events
                NamedPipeServer.Instance.Stop();

                // Kill the Injector process if it's still alive
                if (!proc.Process.HasExited)
                {
                    Console.WriteLine("Killing injector process with pid " + proc.Process.Id);
                    proc.Process.Kill();
                }

                ProgramStatus.Instance.NotifyInjectorExited();

                // Kill any other process spawned by us
                var pids = ProgramStatus.Instance.Pids;
                foreach (var p in pids)
                {
                    try
                    {
                        var child = Process.GetProcessById((int)p);
                        child.Kill();
                        Console.WriteLine("Killed child process with pid " + child.Id);
                    }
                    catch (Exception e)
                    {
                        // The process may already be dead. Ignore it.
                        Console.WriteLine("Error: " + e.Message);
                    }
                }

                // Collect info of the system and send them to the remote machine
                var reported = false;
                while (!reported)
                {
                    try
                    {
                        Console.WriteLine("Collecting report...");
                        string reportPath = PrepareReport(proc, DEFAULT_REPORT_PATH);
                        Console.WriteLine("Sending report to HostController...");
                        ReportWork(j, reportPath, Enum.GetName(typeof(InteractionResult), proc.Result));
                        Console.WriteLine("Done.");
                        reported = true;
                    }
                    catch (Exception e)
                    {
                        // In this case we want to keep trying. It is important to let the Controller know that the job was not ok.
                        Console.WriteLine("Cannot report work back to host controller. I will retry in a moment. Error: " + e.Message + ". " + e.StackTrace);
                        Thread.Sleep(ACQUIRE_WORK_SLEEP_SECS * 1000);
                        reported = false;
                    }
                }

                // Done!
                Console.WriteLine("Job completed!");

                // We finished the test successfully. 
                // In case this was a bare-metal VM, we need to reboot now. 
                // In case of a VM, the situation may be different. For performance
                // reasons it would be nice to start again from a known snapshot
                // simply reverting the VM instead of full reboot. 
                // TODO: add a flag to the job-message so the worker knows if it is supposed to reboot automatically or not.

                // The following statement is necessary in case of bare-metal implementations
                //NativeMethods.Reboot();
                // This one is necessary with VM-only.
                MessageBox.Show("WORK DONE. WAITING FOR REBOOT OPERATION BY THE HOST CONTROLLER...");


                keepRunning = false;
            }
        }

        private void TimeoutReached(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timeout = true;
            Console.WriteLine("TimeoutWatcher: TIMEOUT EXPIRED");
            _interactionTimer.Stop();
            Program.SetTimeoutExpired();

            // Kill everything
            try
            {
                // TODO: for some reason this is not working.
                var pids = ProgramStatus.Instance.Pids;
                foreach (var p in pids)
                {
                    try
                    {
                        Console.WriteLine("TimeoutWatcher: Killing child process with pid " + p);
                        var child = Process.GetProcessById((int)p);
                        child.Kill();
                        // Wait 3 seconds before going with the next one.
                        child.WaitForExit(3000);
                        Console.WriteLine("TimeoutWatcher: Killed child process with pid " + child.Id);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine("TimeoutWatcher: Cannot kill " + p + ". Process may be already exited. " + ex.Message);
                        // According to the MSDN this happens when the process we want to terminate is already gone.
                        ProgramStatus.Instance.RemovePid(p);
                    }
                    catch (Exception ex)
                    {
                        // The process may already be dead. Ignore it.
                        Console.WriteLine("TimeoutWatcher: Cannot kill " + p + ". " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Do nothing
                Console.WriteLine("TimeoutWatcher: Error occurred. " + ex.Message);
            }

        }

        private List<string> CheckNewPrograms()
        {
            List<string> res = new List<string>();

            var current = Program.ListInstalledPrograms();
            foreach (var c in current)
            {
                if (!_originalList.Contains(c))
                    res.Add(c);
            }

            return res;
        }

        /// <summary>
        /// Will only serialize object properties properties.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="parent"></param>
        private void DummySerialize(object o, XmlElement parent)
        {
            if (o == null || parent == null)
                return;

            // The stupid C# default XMLSerializer won't work without parameterless constructor. We need to implement
            // our Serializer using reflection
            var props = o.GetType().GetProperties();
            if (props == null)
                return;

            foreach (var p in props)
            {
                XmlElement e = parent.OwnerDocument.CreateElement(p.Name);
                object val = null;
                try
                {
                    val = p.GetValue(o, null);
                }
                catch (Exception ex)
                {
                    val = null;
                }

                if (val != null)
                {
                    // Check if this object is an instance of a primitive type. If not, apply recursion.
                    if (val.GetType().IsPrimitive || val.GetType() == typeof(string))
                    {
                        e.InnerText = val.ToString();
                    }
                    // Check if this is a collection of items
                    else if (val is System.Collections.IEnumerable)
                    {
                        foreach (var el in val as System.Collections.IEnumerable)
                        {
                            XmlElement litem = parent.OwnerDocument.CreateElement(p.Name + "_Item");
                            DummySerialize(el, litem);
                            e.AppendChild(litem);
                        }
                    }
                    // Otherwise apply recursion
                    else {
                        DummySerialize(val, e);
                    }
                }
                else
                    e.InnerText = "";

                parent.AppendChild(e);
            }
        }

        private string PrepareReport(ProcessContainer p, string outfile)
        {
            var log = Program.GetInstallerLog();

            // Add information on this specific run
            var experiment = log.OwnerDocument.CreateElement("Experiment");
            var installerName = log.OwnerDocument.CreateElement("InstallerName");
            installerName.InnerText = p.Job.LocalFullPath;
            var installerDetails = log.OwnerDocument.CreateElement("InstallerDetails");
            var info = p.Job.FileVersionInfo;
            DummySerialize(info, installerDetails);
            experiment.AppendChild(installerName);
            experiment.AppendChild(installerDetails);
            //var jobId = log.OwnerDocument.CreateElement("JobId");
            //jobId.InnerText = p.Job.Id.ToString();
            //experiment.AppendChild(jobId);
            // Time info: start, stop, elapsed
            var startTime = log.OwnerDocument.CreateElement("StartedOn");
            startTime.InnerText = p.Job.StartTime.ToLongTimeString();
            experiment.AppendChild(startTime);
            var endTime = log.OwnerDocument.CreateElement("EndedOn");
            endTime.InnerText = p.Job.EndTime.ToLongTimeString();
            experiment.AppendChild(endTime);
            var duration = log.OwnerDocument.CreateElement("Duration");
            duration.InnerText = (p.Job.EndTime - p.Job.StartTime).ToString();
            experiment.AppendChild(duration);
            log.PrependChild(experiment);

            // Add information about the specific GuestController configuration
            var guestconf = log.OwnerDocument.CreateElement("GuestConfiguration");
            var jobtimeout = log.OwnerDocument.CreateElement("JobTimeout");
            jobtimeout.InnerText = "" + Settings.Default.EXECUTE_JOB_TIMEOUT;
            guestconf.AppendChild(jobtimeout);
            var hostcontrollerip = log.OwnerDocument.CreateElement("HostControllerIp");
            hostcontrollerip.InnerText = "" + ProgramStatus.Instance.HostControllerAddr.Address.ToString();
            guestconf.AppendChild(hostcontrollerip);
            var hostcontrollerport = log.OwnerDocument.CreateElement("HostControllerPort");
            hostcontrollerport.InnerText = "" + ProgramStatus.Instance.HostControllerPort;
            guestconf.AppendChild(hostcontrollerport);
            var macaddr = log.OwnerDocument.CreateElement("Mac");
            macaddr.InnerText = GetMACAddr();
            var ipaddrs = log.OwnerDocument.CreateElement("GuestIPs");
            foreach (var ip in GetNetworkInfo().GetIPProperties().UnicastAddresses)
            {
                var ipaddr = log.OwnerDocument.CreateElement("IP");
                ipaddr.InnerText = ip.Address.ToString();
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipaddr.SetAttribute("netmask", ip.IPv4Mask.ToString());
                }
                ipaddrs.AppendChild(ipaddr);
            }
            guestconf.AppendChild(ipaddrs);
            var networkinfo = log.OwnerDocument.CreateElement("NetworkInfo");
            DummySerialize(GetNetworkInfo().GetIPProperties(), networkinfo);
            guestconf.AppendChild(networkinfo);
            log.AppendChild(guestconf);

            var result = log.OwnerDocument.CreateElement("Result"); // Main element containing all the result info
            log.AppendChild(result);

            // Start collecting info and produce a nice report
            string errors = p.Process.StandardError.ReadToEnd();
            string output = p.Process.StandardOutput.ReadToEnd();
            int rtnCode = p.Process.ExitCode;

            // Add the Injector return code, stdout and stderr
            var injector = log.OwnerDocument.CreateElement("Injector");
            var stdout = log.OwnerDocument.CreateElement("StdOut");
            stdout.InnerText = output;
            injector.AppendChild(stdout);

            var stderr = log.OwnerDocument.CreateElement("StdErr");
            stderr.InnerText = errors;
            injector.AppendChild(stderr);

            var retcode = log.OwnerDocument.CreateElement("RetCode");
            retcode.InnerText = "" + rtnCode;
            injector.AppendChild(retcode);
            result.AppendChild(injector);

            // Add the MITM info
            var mitm = log.OwnerDocument.CreateElement("MitmAttack");
            var mitm_res = log.OwnerDocument.CreateElement("Success");
            mitm_res.InnerText = ProgramStatus.Instance.MitmSucceded.ToString();
            mitm.AppendChild(mitm_res);

            // Retrieve info from the logged files as well...
            // local_info.log
            int? process_id = null;
            bool? elevated = null;
            string image_path = null;
            string image_sha1 = null;

            if (ProgramStatus.Instance.MitmSucceded)
            {
                string localinfo_file = Path.Combine(Path.GetTempPath(), "local_info.log");
                if (File.Exists(localinfo_file))
                {
                    using (var s = File.OpenText(localinfo_file))
                    {
                        // local_info.log
                        process_id = int.Parse(s.ReadLine());
                        elevated = int.Parse(s.ReadLine()) == 1;
                        image_path = s.ReadLine();
                    }
                }
            }

            if (image_path != null && File.Exists(image_path))
            {
                image_sha1 = Utils.CalculateHash(image_path).sha1;
            }
                
            var mitm_proc_id = log.OwnerDocument.CreateElement("ProcessId");
            mitm_proc_id.InnerText = process_id == null ? "" : process_id.ToString();
            mitm.AppendChild(mitm_proc_id);

            var mitm_proc_elevated = log.OwnerDocument.CreateElement("ProcessElevated");
            mitm_proc_elevated.InnerText = elevated == null ? "" : elevated.ToString();
            mitm.AppendChild(mitm_proc_elevated);

            var mitm_proc_path = log.OwnerDocument.CreateElement("ProcessPath");
            mitm_proc_path.InnerText = image_path == null ? "" : image_path.ToString();
            mitm.AppendChild(mitm_proc_path);

            var mitm_proc_sha1 = log.OwnerDocument.CreateElement("ProcessImageSha1");
            mitm_proc_sha1.InnerText = image_sha1 == null ? "" : image_sha1.ToString();
            mitm.AppendChild(mitm_proc_sha1);

            // mitm.log
            string mitm_file = Path.Combine(Path.GetTempPath(), "mitm.log");
            if (File.Exists(mitm_file))
                using (var s = File.OpenText(mitm_file))
                {
                    var mitm_network_log = log.OwnerDocument.CreateElement("NetworkInfo");
                    mitm_network_log.InnerXml = s.ReadToEnd();
                    mitm.AppendChild(mitm_network_log);
                }
            
            result.AppendChild(mitm);

            // UIBot results
            var uiResult = log.OwnerDocument.CreateElement("UiBot");
            var uiResultDescription = log.OwnerDocument.CreateElement("Description");
            var uiResultValue = log.OwnerDocument.CreateElement("Value");
            uiResultDescription.InnerText = Enum.GetName(typeof(InteractionResult), p.Result);
            uiResultValue.InnerText = ((int)p.Result).ToString();
            uiResult.AppendChild(uiResultValue);
            uiResult.AppendChild(uiResultDescription);
            result.AppendChild(uiResult);

            // Process hierarchy
            var procHierarchy = ProgramStatus.Instance.ProcHierarchy.GetHierarchy(log.OwnerDocument);
            result.AppendChild(procHierarchy);

            // Regsitry access
            var regAccess = log.OwnerDocument.CreateElement("RegistryWriteAccess");
            var accesses = ProgramStatus.Instance.RegAccessLog;
            foreach (var k in accesses)
            {
                // Calculate the diffs.
                k.UpdateDifferences();

                var fl = log.OwnerDocument.CreateElement("Key");
                fl.SetAttribute("Path", k.FullName);
                fl.SetAttribute("Deleted", k.IsDeleted.ToString());
                fl.SetAttribute("Modified", k.IsModified.ToString());
                fl.SetAttribute("New", k.IsNew.ToString());

                if (k.IsNew || k.IsModified || k.IsDeleted)
                {
                    // List all the new VALUES created alongside this key.
                    var new_values = log.OwnerDocument.CreateElement("NewValues");
                    foreach (var kv in k.NewValues)
                    {
                        var key_value = log.OwnerDocument.CreateElement("KeyValue");
                        var k_name = log.OwnerDocument.CreateElement("Name");
                        var k_val = log.OwnerDocument.CreateElement("Value");
                        k_name.InnerText = kv.Key.ToString();
                        k_val.InnerText = kv.Value.ToString();

                        key_value.AppendChild(k_name);
                        key_value.AppendChild(k_val);
                        new_values.AppendChild(key_value);
                    }
                    fl.AppendChild(new_values);

                    // List all the new SUBKEYS created alongside this key.
                    var new_subkeys = log.OwnerDocument.CreateElement("NewSubKeys");
                    foreach (var kv in k.NewSubeys)
                    {
                        var key_value = log.OwnerDocument.CreateElement("Key");
                        key_value.InnerText = kv;
                        new_values.AppendChild(key_value);
                    }
                    fl.AppendChild(new_subkeys);

                    var old_values = log.OwnerDocument.CreateElement("OldValues");
                    foreach (var kv in k.DeletedValues)
                    {
                        var key_value = log.OwnerDocument.CreateElement("KeyValue");
                        var k_name = log.OwnerDocument.CreateElement("Name");
                        var k_val = log.OwnerDocument.CreateElement("Value");
                        k_name.InnerText = kv.Key.ToString();
                        k_val.InnerText = kv.Value.ToString();

                        key_value.AppendChild(k_name);
                        key_value.AppendChild(k_val);
                        old_values.AppendChild(key_value);
                    }
                    fl.AppendChild(old_values);

                    // List all the new SUBKEYS created alongside this key.
                    var old_subkeys = log.OwnerDocument.CreateElement("OldSubKeys");
                    foreach (var kv in k.DeletedSubkeys)
                    {
                        var key_value = log.OwnerDocument.CreateElement("Key");
                        key_value.InnerText = kv;
                        old_subkeys.AppendChild(key_value);
                    }
                    fl.AppendChild(old_subkeys);

                    var edited_values = log.OwnerDocument.CreateElement("EditedValues");
                    foreach (var kv in k.ModifiedValues)
                    {
                        var key_value = log.OwnerDocument.CreateElement("KeyValue");
                        var k_name = log.OwnerDocument.CreateElement("Name");
                        var k_val = log.OwnerDocument.CreateElement("OriginalValue");
                        var k_new_val = log.OwnerDocument.CreateElement("NewValue");

                        k_name.InnerText = kv.Key.ToString();
                        k_val.InnerText = ((dynamic)(kv.Value)).OriginalValue.ToString();
                        k_new_val.InnerText = ((dynamic)(kv.Value)).CurrentValue.ToString();

                        key_value.AppendChild(k_name);
                        key_value.AppendChild(k_val);
                        key_value.AppendChild(k_new_val);
                        edited_values.AppendChild(key_value);
                    }
                    fl.AppendChild(edited_values);

                    var edited_subkeys = log.OwnerDocument.CreateElement("EditedSubKeys");
                    fl.AppendChild(edited_subkeys);

                    regAccess.AppendChild(fl);
                }

            }
            result.AppendChild(regAccess);

            // New/Modified/Deleted/Renamed files
            var fileAccess = log.OwnerDocument.CreateElement("FileAccess");
            var newFiles = fileAccess.OwnerDocument.CreateElement("NewFiles");
            var modifiedFiles = fileAccess.OwnerDocument.CreateElement("ModifiedFiles");
            var deletedFiles = fileAccess.OwnerDocument.CreateElement("DeletedFiles");
            var otherFiles = fileAccess.OwnerDocument.CreateElement("OtherAccessFiles");
            var files = ProgramStatus.Instance.FileAccessLog;
            fileAccess.SetAttribute("count", files.Count().ToString());

            Parallel.ForEach(files, (file) =>
            {
                // Note: we need to invoke this method before all the others.
                // This triggers some last checks on the file and freezes its history.
                var records = file.CheckoutHistory();

                bool newfile = file.IsNew();
                bool modifiedfile = file.IsModified();
                bool deletedfile = file.IsDeleted();
                bool leftOver = file.LeftOver();

                var fl = log.OwnerDocument.CreateElement("File");
                fl.SetAttribute("Path", file.Path);
                fl.SetAttribute("Deleted", deletedfile.ToString());
                fl.SetAttribute("Modified", modifiedfile.ToString());
                fl.SetAttribute("New", newfile.ToString());
                fl.SetAttribute("LeftOver", leftOver.ToString());
                var history = log.OwnerDocument.CreateElement("AccessHistory");
                // Build the story of the file
                int count = 0;
                foreach (var record in records)
                {
                    var erec = log.OwnerDocument.CreateElement("FileStatus");
                    erec.SetAttribute("Sequence", count.ToString());

                    var path = log.OwnerDocument.CreateElement("Path");
                    path.InnerText = record.path;
                    erec.AppendChild(path);

                    var sha1_hash = log.OwnerDocument.CreateElement("Sha1Hash");
                    sha1_hash.InnerText = record.hashes.sha1;
                    erec.AppendChild(sha1_hash);

                    var md5_hash = log.OwnerDocument.CreateElement("Md5Hash");
                    md5_hash.InnerText = record.hashes.md5;
                    erec.AppendChild(md5_hash);

                    var fuzzyHash = log.OwnerDocument.CreateElement("FuzzyHash");
                    fuzzyHash.InnerText = record.fuzzyHash;
                    erec.AppendChild(fuzzyHash);

                    var size = log.OwnerDocument.CreateElement("Size");
                    size.InnerText = record.size.ToString();
                    erec.AppendChild(size);

                    var exists = log.OwnerDocument.CreateElement("Exists");
                    exists.InnerText = record.exists.ToString();
                    erec.AppendChild(exists);

                    history.AppendChild(erec);
                    count++;
                }
                fl.AppendChild(history);
                if (file.LeftOver())
                {
                    if (Settings.Default.PERFORM_STRING_ANALYSIS)
                    {
                        // Calculate the strings for that file
                        Process strings = new Process();
                        strings.StartInfo.FileName = "strings/strings.exe";
                        strings.StartInfo.UseShellExecute = false;
                        strings.StartInfo.Arguments = String.Format("-n {0} {1} /accepteula", Settings.Default.STRINGS_MIN_LEN, file.Path);
                        strings.StartInfo.RedirectStandardOutput = true;
                        strings.Start();

                        var stringAnalysis = log.OwnerDocument.CreateElement("Strings");
                        stringAnalysis.SetAttribute("MinLength", Settings.Default.STRINGS_MIN_LEN.ToString());
                        using (var r = strings.StandardOutput)
                        {
                            // Use an hashset to remove duplicate values
                            HashSet<string> string_set = new HashSet<string>();
                            while (!r.EndOfStream)
                            {
                                // Truncate to 400 chars each string.
                                string str = null;
                                str = r.ReadLine();
                                if (str.Length > 400)
                                    str = str.Substring(0, 400);
                                string_set.Add(str);
                            }

                            // Add the strings into the document
                            foreach (var s in string_set)
                            {
                                var estr = log.OwnerDocument.CreateElement("String");
                                estr.InnerText = s;
                                stringAnalysis.AppendChild(estr);
                            }
                        }
                        fl.AppendChild(stringAnalysis);
                        strings.WaitForExit();
                        // In case the string command failed, remove the nodes.
                        if (strings.ExitCode != 0)
                            stringAnalysis.RemoveAll();
                    }

                    newFiles.AppendChild(fl);
                }
                else if (deletedfile)
                    deletedFiles.AppendChild(fl);
                else if (modifiedfile)
                {
                    modifiedFiles.AppendChild(fl);
                }
                else
                    otherFiles.AppendChild(fl);
            });

            fileAccess.AppendChild(newFiles);
            fileAccess.AppendChild(modifiedFiles);
            fileAccess.AppendChild(deletedFiles);
            fileAccess.AppendChild(otherFiles);

            result.AppendChild(fileAccess);

            // New application detected
            var deltaApps = log.OwnerDocument.CreateElement("NewApplications");
            var newProgs = CheckNewPrograms();
            deltaApps.SetAttribute("count", newProgs.Count.ToString());
            foreach (var s in newProgs)
            {
                var app = log.OwnerDocument.CreateElement("Application");
                app.InnerText = s;
                deltaApps.AppendChild(app);
            }
            result.AppendChild(deltaApps);

            // Now collect logs and other info, like screenshots.
            ProgramLogger.Instance.Close();
            var appLog = log.OwnerDocument.CreateElement("AppLog");
            appLog.InnerText = File.ReadAllText(ProgramLogger.Instance.GetLogFile());
            log.AppendChild(appLog);

            string f = ZipScreens();
            // Add the zip file to the report
            var screens = log.OwnerDocument.CreateElement("InteractionScreenshots");
            screens.InnerText = Convert.ToBase64String(File.ReadAllBytes(f)); // This Kills memory!!! //TODO //FIXME
            log.AppendChild(screens);

            
            // Write the collected info to a local report.xml file.
            using (var fs = File.Create(outfile))
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(fs, Encoding.UTF8))
                {
                    xmlWriter.Formatting = System.Xml.Formatting.Indented;
                    log.WriteTo(xmlWriter);

                    // Flush changes on disk.
                    fs.Flush();
                }
            }

            return outfile;

        }

        private static void CopyStream(Stream source, Stream target)
        {
            const int bufSize = 0x1000;
            byte[] buf = new byte[bufSize];
            int bytesRead = 0;
            while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
                target.Write(buf, 0, bytesRead);
        }// end:CopyStream()

        private string ZipScreens()
        {
            string fpath = Path.GetTempFileName() + ".zip";
            using (ZipFile zip = new ZipFile())
            {
                // add this map file into the "images" directory in the zip archive
                zip.AddDirectory(Settings.Default.INTERACTIONS_SCREEN_PATH);
                zip.Save(fpath);
            }
            return fpath;
        }

        private string GetMACAddr()
        {

            string macAddr = GetNetworkInfo().GetPhysicalAddress().ToString();

            // Add separators to the mac
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < macAddr.Length; i++)
            {
                if ((i % 2) == 0 && i != 0)
                    builder.Append(':');
                builder.Append(macAddr[i]);
            }

            return builder.ToString();

        }

        private NetworkInterface GetNetworkInfo()
        {
            return
                (
                    from nic in NetworkInterface.GetAllNetworkInterfaces()
                    where nic.OperationalStatus == OperationalStatus.Up
                    select nic
                ).FirstOrDefault();
        }

        private Window WaitForInputRequested()
        {
            // There's no progragmatic way to understand that, so I'll apply a sort of logic strategy
            /*
             * From the User's point of view, his action is requested when the UI is static. If nothing is moving
             * then the user would think his action is requested. For this reason, I apply this simple strategy:
             * 1. Detect the most-probable window handle of all the monitored PIDs.
             * 2. Scan it by taking a screenshot and calculating an hash which will be memorized
             * 3. Reiterate again until I find the same hash for at least 4 times.
             * 4. Each time I reiterate, wait for half a second, so the process has time to update itself.
             * If no window is found, return null.
             * 
             * NOTES: Loops detection.
             * It may happen that a window is presenting the same sequence of frames, such as an animation.
             * This method tries to handle this situation by tracking the hashes of the frames into a dictionary
             * with an associated counter. Whenever a new frame is detected, all the counters are resetted. 
             * So, only when no new frames are detected and there is a counter > LOOP_THRESHOLD, the window
             * is considered to be stable.
             * 
             * TODO
             * Please note that this mechanism could be heavily improved by hooking Win32 Drawing APIs to trigger
             * the analysis when needed and not by waiting an arbitrary amount of time. This is something I can
             * improve in future versions.
             * 
             * TODO2
             * Note that I am not taking track of stable window HASHES. It may be a good idea to store
             * them in a dictionary to detect loops (Abort->Cancel->Abort->Cancel->Abort->Cancel...)
             */
            int LOOP_THRESHOLD = 4;
            int stableScans = 0;

            IntPtr prevHandle = IntPtr.Zero;
            Window currentWindow = null;
            Window prevWindow = null;
            string prevHash = null;
            // Use a dictionary to detect possible loops. 
            Dictionary<string, int> previousHashes = new Dictionary<string, int>();

            // Loop until we reach a minimum number of stable screenshots
            while (stableScans <= LOOP_THRESHOLD && !_timeout)
            {
                // Check if we are into a loop. For us a loop means stability!
                bool loopDetected = false;
                foreach (var k in previousHashes)
                {
                    if (k.Value > LOOP_THRESHOLD)
                    {
                        loopDetected = true;
                        break;
                    }
                }
                if (loopDetected)
                    break;

                // Guess the UI window handle
                IntPtr wH = GetProcUIWindowHandle();

                // If I found no windows, return null immediatly.
                if (wH == IntPtr.Zero)
                {
                    Console.WriteLine("WaitInputRequest: NO WINDOW found!");
                    Thread.Sleep(REACTION_TIMEOUT);
                    /*
                    prevHandle = wH;
                    stableScans = 0;
                    Thread.Sleep(pollInterval);
                    continue;
                     * */
                    return null;
                }

                // If the Handle of the mainWindow changes, reset the stability counters and iterate again.
                if (!wH.Equals(prevHandle))
                {
                    previousHashes.Clear();
                    prevHandle = wH;
                    stableScans = 0;
                    Thread.Sleep(REACTION_TIMEOUT);
                    continue;
                }

                // Create a Window Object using the hWND just obtained.
                // If this is the first time we detect a window, set the previous window as this one, wait a bit and loop again
                currentWindow = new Window(wH);
                if (prevWindow == null)
                {
                    prevWindow = currentWindow;
                    prevHash = UIAnalysis.NativeAndVisualRanker.CalculateHash(prevWindow);

                    if (prevHash != null)
                    {
                        previousHashes.Add(prevHash, 1);


                        // Reset all the other counters
                        for (int i = previousHashes.Count - 1; i >= 0; i--)
                        {
                            var k = previousHashes.ElementAt(i).Key;
                            previousHashes[k] = 1;
                        }
                    }

                    stableScans = 0;
                    Thread.Sleep(REACTION_TIMEOUT);
                    continue;
                }


                // Here we have the situation in which prevWindow is not null and currentWindow is neither. 
                // So I need to check weather there are visual differences between the two windows.
                string currentHash = UIAnalysis.NativeAndVisualRanker.CalculateHash(currentWindow);
                if (prevHash == null)
                {
                    prevHash = currentHash;
                    stableScans = 0;
                    Thread.Sleep(REACTION_TIMEOUT);
                    continue;
                }

                if (currentHash != null && prevHash != currentHash)
                {
                    // Something changed
                    prevWindow = currentWindow;
                    prevHash = currentHash;

                    // Update the loop counter accordingly
                    if (previousHashes.ContainsKey(currentHash))
                        previousHashes[currentHash]++;
                    else
                    {
                        previousHashes.Add(currentHash, 1);

                        // Reset all the other counters
                        for (int i = previousHashes.Count - 1; i >= 0; i--)
                        {
                            var k = previousHashes.ElementAt(i).Key;
                            previousHashes[k] = 1;
                        }
                    }

                    // Reset the counter and loop again
                    stableScans = 0;
                    Thread.Sleep(REACTION_TIMEOUT);
                    continue;
                }

                // Is there any progressbar? if yes, proceed only if it is 100% completed...
                // What about CPU utilization of the process???
                // TODO

                // Ok, after all the check above, I can assume the window UI has not changed. Increase the counter.
                if (previousHashes.ContainsKey(prevHash))
                    previousHashes[prevHash]++;
                else
                    previousHashes.Add(prevHash, 0);

                stableScans++;
                Thread.Sleep(REACTION_TIMEOUT);

            }
            // The window looks like stable, return it.
            return currentWindow;
        }

        /// <summary>
        /// By looking at the list of ProcessIDs (ProgramStatus.PIDS) to monitor, this function
        /// tries to guess the Window representing the UI.
        /// </summary>
        /// <returns>Guessed hWND of the UI Window, or IntPtr.Zero if no window can be found.</returns>
        private IntPtr GetProcUIWindowHandle()
        {
            var mypids = ProgramStatus.Instance.Pids;

            // Proceed using AutomationElement, maybe we find something more.
            AutomationElement winner = null;
            // List all the processes that currently have a non null WindowHandle
            List<Condition> pidsCond = new List<Condition>();
            for (int i = 0; i < mypids.Length; i++)
            {
                Process proc = null;
                try
                {
                    proc = Process.GetProcessById((Int32)mypids.ElementAt(i));
                    if (proc.MainWindowHandle == IntPtr.Zero)
                        continue;
                    else
                    {
                        var pidcond = new PropertyCondition(AutomationElement.ProcessIdProperty, proc.Id);
                        pidsCond.Add(pidcond);
                    }
                }
                catch (Exception e)
                {
                    // The process might be dead.
                    continue;
                }
            }


            // If there is no pid to monitor, we will have no results. So, return now.
            if (pidsCond.Count > 0)
            {
                if (pidsCond.Count == 1)
                {
                    // The or condition will require 2 conditions to work. Add a False condition to shut its mouth
                    pidsCond.Add(Condition.FalseCondition);
                }

                // Use UIAtuomation to query the windows available matching all the resutls (belonging to our pids, visibility).
                var pidsInOr = new OrCondition(pidsCond.ToArray());
                var windowVisible = new PropertyCondition(AutomationElement.IsOffscreenProperty, false);
                var cond = new AndCondition(pidsInOr, windowVisible);

                var allae = AutomationElement.RootElement.FindAll(TreeScope.Children, cond);

                // If we have some result, check if there is any focused element or some "golden" one we might prefer...
                if (allae != null && allae.Count > 0)
                {
                    StringBuilder test = new StringBuilder();
                    foreach (AutomationElement a in allae)
                    {
                        // Log all the result windows and chose the one with focus.
                        test.AppendLine(a.Current.ControlType + " : " + a.Current.BoundingRectangle + " : " + a.Current.BoundingRectangle);
                        if (a.Current.HasKeyboardFocus)
                        {
                            winner = a;
                        }
                    }

                    // If none had the focus, grab the first. 
                    // TODO: we might use some heuristic here to get a better window, such as dimensions, colors, etc.
                    if (winner == null)
                    {
                        winner = allae[0];
                    }
                }
                else
                {
                    // Sometime, it may happen that the UI is just minimized. Why don't we meximize it again?
                    foreach (var pid in mypids)
                    {
                        try
                        {
                            var aprocess = Process.GetProcessById((int)pid);
                            NativeMethods.ShowWindow(aprocess.MainWindowHandle, NativeMethods.ShowWindowCommands.Normal);
                        }
                        catch (Exception e)
                        {
                            // Tons of things may go wrong here. Just ignore them.
                        }
                    }
                }
            }

            // If we still have no window, try to get them via the WindowsNativeApi
            if (winner != null)
            {
                return new IntPtr(winner.Current.NativeWindowHandle);
            }
            else {
                var foreground = NativeMethods.GetForegroundWindow();
                uint processId = 0;
                GetWindowThreadProcessId(foreground, out processId);
                for (int i = 0; i < mypids.Length; i++)
                {
                    if (processId == mypids[i])
                    {
                        // This is our main window!
                        return foreground;
                    }
                }
            }

            // If still no window has been found, return 0
            return IntPtr.Zero;
        }

        private ProcessContainer StartProcessWithInjector(Job j)
        {
            ProcessContainer res = null;
            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = Properties.Settings.Default.INJECTOR_IMAGE_NAME;
                proc.StartInfo.Arguments = "\"" + j.LocalFullPath + "\"";
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.UseShellExecute = false;
                res = new ProcessContainer(proc, j);
                Console.WriteLine("UI Bot: \nstarting " + proc.StartInfo.FileName + "\nArgs: " + proc.StartInfo.Arguments);
                res.Start();
                Console.WriteLine("UI Bot: Started");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
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
            int status = NativeMethods.NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
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
