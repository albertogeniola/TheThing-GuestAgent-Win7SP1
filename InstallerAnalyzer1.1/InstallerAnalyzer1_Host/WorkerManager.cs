using InstallerAnalyzer1_Host.Properties;
using NetworkProtocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace InstallerAnalyzer1_Host
{

    class WorkerManager
    {
        #region ----------- singleton -----------

        /// <summary>
        /// Pattern singleton
        /// </summary>
        private static WorkerManager _instance;

        /// <summary>
        /// Pattern Singleton: this getter will instantiate the instance if null.
        /// </summary>
        public static WorkerManager Instance
        {
            get {
                if (_instance == null)
                {
                    _instance = new WorkerManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor, singleton pattern.
        /// </summary>
        private WorkerManager()
        {
            _workers = new List<NetworkWorker>();
        }

        #endregion

        private Thread _listenerThread;
        private List<NetworkWorker> _workers;
        private System.Net.Sockets.TcpListener _listener;
        
        
        private enum NetworkWorkerStatus
        { 
            Ready,
            Busy
        }

        /// <summary>
        /// This methods creates and starts a thread which will manage network communication
        /// </summary>
        public void Start()
        {
            if (_listenerThread == null || !_listenerThread.IsAlive)
            {
                _listenerThread = new Thread(new ThreadStart(Run));
                _listenerThread.Start();
            }
            else
            {
                throw new ApplicationException("Error: the WorkerManager thread has prevoiusly been started. You cannot start this thread twice!");
            }
        }

        /// <summary>
        /// This method takes care of network communication: wait for incoming connection and spawns NetworkWorkers
        /// </summary>
        private void Run()
        {
            _listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, int.Parse(Settings.Default.HOST_SERVER_PORT));
            _listener.Start();
            while (true)
            {
                TcpClient c = _listener.AcceptTcpClient();
                NetworkWorker w = new NetworkWorker(c);
                _workers.Add(w);
                ((IWorker)w).Start();
            }
        }
        
        /// <summary>
        /// This class represent a server spawned and ready to server client's requests
        /// A network worker will manage the communication with the network computer which is going 
        /// to perform the requested operations. 
        /// </summary>
        class NetworkWorker : IWorker, IDisposable
        {
            private TcpClient _socket;
            private TodoJob _todoJob;
            private Job _mainJob;
            private Thread _thread;
            private BinaryWriter _sWriter;
            private BinaryReader _sReader;
            private NetworkWorkerStatus _status;
            private DBManager _dbm;
            private Object _lock = new Object();
            private List<string> _noLoop;
            private string _vmName;
            private TextWriter _fileLogger;
            private List<string> _followedPathBitmaps;

            private struct PossibleInteraction {
                public int winHandler;
                public byte[] bitmap;
                public string winStatusHash;
                public int interactiveControlN;
                
                public string[] ids;
                public int[] interactionId;
                public double[] topLeftX;
                public double[] topLeftY;
                public double[] bottomRightX;
                public double[] bottomRightY;
                public string[] texts;
                public bool[] focused;

                public override bool Equals(object obj)
                {
                    if (!(obj is PossibleInteraction))
                        return false;


                    PossibleInteraction b = (PossibleInteraction)obj;
                    /*
                    if (winHandler != b.winHandler)
                        return false;

                    if (interactiveControlN != b.interactiveControlN)
                        return false;

                    if (ids.Length != b.ids.Length)
                        return false;

                    if (interactions.Length != b.interactions.Length)
                        return false;

                    for (int i = 0; i < interactiveControlN; i++)
                    {
                        if (ids[i] != b.ids[i])
                            return false;

                        if (interactions[i].Length != b.interactions[i].Length)
                            return false;

                        for (int j = 0; j < interactions[i].Length; j++)
                        {
                            if (interactions[i][j] != b.interactions[i][j])
                                return false;
                        }
                    }
                    */

                    return b.winStatusHash == winStatusHash;

                    //return true;

                }
            }

            public NetworkWorkerStatus Status
            {
                get { return _status; }
            }
            /// <summary>
            /// Private constructor: a worker can be spawned only if there is a ready connection endpoint and only by the Manager.
            /// </summary>
            public NetworkWorker(TcpClient clientSocket)
            {
                _socket = clientSocket;
                _sReader = new BinaryReader(_socket.GetStream());
                _sWriter = new BinaryWriter(_socket.GetStream());
                _status = NetworkWorkerStatus.Ready;
            }
            void IWorker.Start()
            {
                // Cannot started if already started!
                if (_thread != null && _thread.IsAlive)
                {
                    // Noo!
                    throw new ApplicationException("Error: you cannot start again this thread!");
                }
                // Otherwise, start the thread
                _thread = new Thread(new ThreadStart(Run));
                _thread.Start();
            }
            void IDisposable.Dispose()
            {
                
                //Deallocate TCP connection resources
                _socket.GetStream().Close();
                _socket.Close();
                if (_dbm != null)
                    _dbm.Dispose();
                Log("Thread being disposed.");
                if (_fileLogger != null)
                {
                    _fileLogger.Dispose();
                }
                
            }

            private void Log(string text)
            {
                if (_fileLogger != null)
                {
                    _fileLogger.WriteLine(DateTime.Now.ToString() + " -- "+ text);
                }
                else
                    lock (Console.Out)
                    {
                        Console.WriteLine(text);
                    }
            }

            /// <summary>
            /// This method represent the first part of the protocol, which is always executed every time the VM connects to this. It consists in sending the installer file and wait until the remote
            /// machine starts the executable.
            /// </summary>
            private void SendWork()
            {
                // 1.0 Send File Name
                FileInfo fi = new FileInfo(_mainJob.InstallerPath);
                // Send FileName
                _sWriter.Write(fi.Name);
                // Send FileSize
                _sWriter.Write(fi.Length);
                // Wait ACK
                Int16 ans = _sReader.ReadInt16();
                if (ans != Protocol.ACK)
                {
                    throw new ProtocolException("Error: I was expecting an ACK from server at #1. Check protocol...");
                }
                // Send Bytes!
                FileStream fs = File.OpenRead(fi.FullName);
                Byte[] buff = new Byte[4096];
                int read = 0;
                while ((read = fs.Read(buff, 0, buff.Length)) > 0)
                {
                    _sWriter.Write(buff, 0, read);
                }
                // Wait Ack
                ans = _sReader.ReadInt16();
                if (ans != Protocol.ACK)
                {
                    throw new ProtocolException("Error: I was expecting an ACK from server at #2. Check protocol...");
                }
                
            }
            
            private string BuildHostXmlError(string customMessage, Exception e, TodoJob todoJob, Job mainJob, string vmName, string followedPath)
            {
                String res = null;

                XmlDocument doc = new XmlDocument();
                XmlElement error = doc.CreateElement("ERROR");
                
                if (!String.IsNullOrEmpty(customMessage))
                    error.SetAttribute("MESSAGE",customMessage);

                if (e!=null)
                {
                    error.SetAttribute("EXCEPTION_MESSAGE",e.Message);
                    error.SetAttribute("EXCEPTION_STACK_TRACE",e.StackTrace);
                }

                if (mainJob!=null)
                    error.SetAttribute("JOB_ID",mainJob.Id.ToString());

                if (todoJob!=null)
                {
                    error.SetAttribute("TODO_JOB_ID",todoJob.Id.ToString());
                    error.SetAttribute("TODO_JOB_PATH", todoJob.ActionPath);
                }
                
                if (!String.IsNullOrEmpty(vmName))
                    error.SetAttribute("VM_NAME",vmName);

                if (!String.IsNullOrEmpty(followedPath))
                    error.SetAttribute("FOLLOWED_PATH",followedPath);

                error.SetAttribute("TIME",DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss"));
                
                doc.AppendChild(error);
                
                using (StringWriter sw = new StringWriter())
                {
                    XmlWriterSettings sett = new XmlWriterSettings() {
                        Encoding=Encoding.Unicode,
                        CheckCharacters=true,
                        Indent=true,
                        OmitXmlDeclaration=false
                    };

                    using (XmlWriter writer = XmlWriter.Create(sw,sett))
                    {
                        doc.WriteContentTo(writer);
                        writer.Flush();
                    }
                    sw.Flush();
                    res = sw.GetStringBuilder().ToString();
                }

                return res;
            }

            // This is the method executed by the thread. It will manage the interaction with the guest process, sending commands and receiving informations about the Intaller UI and logging system.
            private void Run()
            {
                #region Declaration and initializations
                _status = NetworkWorkerStatus.Busy;
                _dbm = DBManagerFactory.NewManager();
                _noLoop = new List<string>();
                _mainJob = null;
                _todoJob = null;
                string followedPath = null;
                _followedPathBitmaps = new List<string>();
                string remoteIp = ((IPEndPoint)_socket.Client.RemoteEndPoint).Address.ToString();
                int remotePort = ((IPEndPoint)_socket.Client.RemoteEndPoint).Port;
                #endregion

                
                /*------------------------------------------------------------
                 * 
                 *                          PHASE 1
                 *         Retrive job from DB and send it to the guest.
                 * 
                 * -----------------------------------------------------------
                 */
                try 
                {
                    #region First Setp: Get The Job from the DB and send it to the remote guest
                    /*-----------------------------------------------------------
                        * ----------------------------------------------------------
                        *         Get the Job from the DB and send it to the Guest
                        * ----------------------------------------------------------
                        * ----------------------------------------------------------*/
                    while (_todoJob == null)
                    {
                        // Now the machine will send a Request every minute to get more work. Expect it!
                        ReceiveCallForWork();

                        lock (_lock)
                        {
                            _todoJob = _dbm.PopTodoJob(out _mainJob);
                        }

                        if (_mainJob == null)
                        {
                            // There's nothing to do... wait please!
                            SendNoJobs();
                            Log("No jobs found for that worker...");
                            Log("Worker thread started with guest " + ((IPEndPoint)_socket.Client.RemoteEndPoint).Address + ":" + ((IPEndPoint)_socket.Client.RemoteEndPoint).Port);
                            _status = NetworkWorkerStatus.Ready;
                        }
                        else
                        {
                            SendWorkReady();
                            // Now the loop will stop
                            // break;
                        }
                    }
                    

                    // Send Work to the remote guest
                    Log("Sending work to remote guest: MAINJOIB ID "+_mainJob.Id+" ("+_mainJob.InstallerPath+"), TODOJOB ID "+_todoJob.Id+" ("+_todoJob.ActionPath+").");
                    SendWork();

                    // Wait for ACK, meaning Process started correctly
                    Log("Waiting for remote process start");
                    ReceiveAck();
                    Log("Remote process started");

                    //Stats.Instance.notifyWork((IPEndPoint)_socket.Client.RemoteEndPoint,_mainJob,_todoJob);

                    #endregion
                }
                catch (Exception e) // Error handling for phase 1
                {
                    // In case of exception, if any job has been retrived, 
                    // remove it from TODO-Table and move it to the done table.
                    // Add error logs both to the local log and to the XML logfile in the DONE table.
                    #region Error handling for phase 1
                    // The job has been popped from db. 
                    // Log the error locally
                    Logger.Instance.logError("Unhandled exception in Worker Manager in phase 1. TodoJob ID = "+_todoJob.Id,e);
                    Log("Unhandled exception in Worker Manager in phase 1. TodoJob ID = "+_todoJob.Id+"\n"+e.Message+"\n"+e.StackTrace);
                    // Log it into the db
                    string xmlErrorLog = BuildHostXmlError("Unhandled exception in Worker Manager in phase 1",e,_todoJob,_mainJob,_vmName,followedPath);
                    if (_todoJob!=null)
                    {
                        // Set that job as done
                        _dbm.InsertDoneWithError(NetworkProtocol.Protocol.URI_APP_START,_mainJob,xmlErrorLog);
                        // Now delete the todo job from the db
                        _dbm.DeleteTodoJob(_todoJob);
                    }

                    // Finally revert the VM
                    VMManager.Instance.RevertVm(_vmName);

                    // Clear stuff and return
                    _dbm.Dispose();
                    _sReader.Close();
                    _sWriter.Close();
                    _socket.Close();

                    return;

                    #endregion
                }


                /*------------------------------------------------------------
                 * 
                 *                          PHASE 2
                 *         Following the stored path of the current todojob.
                 * 
                 * -----------------------------------------------------------
                 */
                followedPath = Protocol.URI_APP_START;
                try 
                {
                    #region Second Step: interact with UI until FollowedPath == todojob.FullPath
                    
                        Log("Following path: "+_todoJob.ActionPath);
                        while (followedPath != _todoJob.ActionPath)
                        {
                            // 1. Check that remote process is running
                            Log("Receiving process status");
                            Int16 procStatus = ReceiveProcessStatus();
                            if (procStatus != Protocol.PROCESS_GOING)
                                // This is unespected, so throw an exception
                                throw new ProtocolException("Error during path following: The remote process is not running.");
                            Log("Receiving possible interactions");
                            // 2. Receive the list of possible interaction to perform with the UI
                            PossibleInteraction res = ReceivePossibleInteractions();

                            // 3. Check if the window has been already scanned: if yes, throw an exception, otherwise add the hash to the scanned windows list
                            if (_noLoop.Contains(res.winStatusHash))
                                throw new LoopException();
                            else
                                _noLoop.Add(res.winStatusHash);

                            // 4. Check if the next action to perform is into the list of the actions received by the Guest
                            TodoJob.Action next = _todoJob.NextAction();
                            /*
                            bool controlFound = false;
                            bool interactionFound = false;
                            for (int i = 0; i < res.ids.Length; i++)
                            {
                                if (next.controlId == res.ids[i])
                                {
                                    controlFound = true;
                                    for (int j = 0; j < res.interactions[i].Length; j++)
                                    {
                                        if (res.interactions[i][j] == next.interactionType)
                                        {
                                            interactionFound = true;
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                            if (!interactionFound)
                            {
                                Log("Cannot follow the written path. The control I was looking for is absent.");
                                // The interaction we were looking for is not present into the collection received by the guest. Throw an exception then!
                                if (!controlFound)
                                    throw new InconsistentPathException("Error: inconsistent path. The guest has no control ID " + next.controlId);
                                else
                                    throw new InconsistentPathException("Error: inconsistent path. The control ID " + next.controlId + " doesn't allow interactiontype " + next.interactionType);
                            }
                            */
                            // 5. Execute the interaction
                            Log("Executing interaction...");
                            if (SendCommand(next.controlId, next.interactionType))
                            {
                                // Mark the choosen element
                                int id = -1;
                                for (int i = 0; i < res.ids.Length; i++)
                                {
                                    if (next.controlId == res.ids[i])
                                    {
                                        id = i;
                                        break;
                                    }
                                }
                                if (id == -1)
                                    throw new ApplicationException("Inconsistency error: no control id found within the RES array.");
                                DrawAndStoreBitmap(res.bitmap, (int)res.topLeftX[id], (int)res.topLeftY[id],(int) (res.bottomRightX[id] - res.topLeftX[id]),(int)(res.topLeftY[id] - res.bottomRightY[id]));
                                // Everything is good, update the path
                                followedPath += next.relativePath + Protocol.URI_PATH_SEP;
                                Log("Interaction completed succesfully. New path is: "+followedPath);
                            }
                            else
                                throw new GuestCommandFailedException("Guest reported a problem when executing the action " + next.controlId + "=" + next.interactionType);

                        }
                    #endregion
                }
                catch (Exception e)
                {
                    // If a loop is detected, save the path in DONE table with an appropriate error message. Then remove that TODO entry and all the others
                    // starting like that from the TODO table. After that,simply ask the remote guest to reboot.
                    #region Error Handling for phase 2
                    Log("Loop exception catched during Pahse 2.");
                    string xmlError = BuildHostXmlError("Loop detected during phase 2.",e,_todoJob,_mainJob,_vmName,followedPath);
                    Log("Inserting done job to the DB");
                    _dbm.InsertDoneWithError(NetworkProtocol.Protocol.URI_APP_START,_mainJob,xmlError);
                    Log("Deleting todojob starting with: " + followedPath);
                    _dbm.DeleteTodoJobsStartingWithPath(followedPath);
                    Log("Deleting this todojob: " + _todoJob.ActionPath);
                    _dbm.DeleteTodoJob(_todoJob);
                    Log("Reverting VM...");
                    VMManager.Instance.RevertVm(_vmName);
                    Stats.Instance.notifyLoop();
                    Log("Clearing resources and returning.");
                    _sReader.Close();
                    _sWriter.Close();
                    _socket.Close();
                    _dbm.Dispose();
                    Log("Returning.");
                    return;
                    #endregion
                }
                    

                /*------------------------------------------------------------
                 * 
                 *                          PHASE 3
                 *                      Discovering paths
                 * 
                 * -----------------------------------------------------------
                 */
                try
                {
                    #region Phase 3: interact with the UI until the remtoe process is running
                    Int16 guestStatus;
                    Log("Path completely followed, now it begins path discovery.");
                    while ((guestStatus = ReceiveProcessStatus()) == Protocol.PROCESS_GOING)
                    {
                        Log("Process status RUNNING received.");
                        // 1. Receive all possible interactions. This means also the Window has will be received.
                        Log("Getting possible interactions...");
                        PossibleInteraction res = ReceivePossibleInteractions();

                        // 2. Check the window hash: if it is already present into the noLoop array, throw a loop exception. Otherwise simply add the has to that array
                        if (_noLoop.Contains(res.winStatusHash))
                            throw new LoopException();
                        else
                            _noLoop.Add(res.winStatusHash);

                        // 3. Now filter all the possible interactions received. Must delete all the already done actions (DONE TABLE), all the ones which are already present into the TODO table.
                        List<string> allPossibleInteractions = BuildPaths(followedPath, res);
                        List<string> toDo = _dbm.FilterDoneOrTodoByPath(allPossibleInteractions);

                        Log("Discovered " + toDo.Count + " new paths.");
                        if (toDo.Count == 0)
                        {
                            Log("I don't know what to do next!!!!!!!!");
                            // There's nothing to do next: Reboot the guest
                            _dbm.DeleteTodoJob(_todoJob);
                            VMManager.Instance.RevertVm(_vmName);
                            //throw new NotSupportedException("Check this: should I add this done path to the DB?");
                            // I guess I should break in that case, collect info and reboot the remote guest
                            // break;
                        }
                        else
                        {
                            // 4. Pop one of the actions filtered and save the others into the TODO table
                            string fullPathToPerform = toDo.First(); // Copied or not?
                            toDo.RemoveAt(0); // Now is fullPathToPerform still valid?
                            foreach (string s in toDo)
                                _dbm.InsertAction(_mainJob.Id, s);
                            // Now proceed with the choosen one
                            string tmp = fullPathToPerform.Split(new string[] { Protocol.URI_PATH_SEP }, StringSplitOptions.RemoveEmptyEntries).Last();
                            string cId = tmp.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[0];
                            int cType = int.Parse(tmp.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                            Log("Saved all the others command to the DB. I'bve choosen the following: " + cId + "=" + cType);
                            if (!SendCommand(cId, cType))
                            {
                                Log("Error during remote command execution... command failed.");
                                throw new GuestCommandFailedException("The guest failed executing command " + cId + "=" + cType);
                            }
                            else
                            {
                                // Mark the choosen element
                                int id = -1;
                                for (int i = 0; i < res.ids.Length; i++)
                                {
                                    if (cId == res.ids[i])
                                    {
                                        id = i;
                                        break;
                                    }
                                }
                                if (id == -1)
                                    throw new ApplicationException("Inconsistency error: no control id found within the RES array.");
                                DrawAndStoreBitmap(res.bitmap, (int)(res.topLeftX[id]), (int)res.topLeftY[id], (int)(res.bottomRightX[id] - res.topLeftX[id]), (int)(res.bottomRightY[id]-res.topLeftY[id]));
                                followedPath += cId + "=" + cType + Protocol.URI_PATH_SEP;
                                Log("Command executed. the remote path is " + followedPath);
                            }
                        }
                    }
                    #endregion
                }
                catch (GuestCommandFailedException e)
                {
                    #region Error handling
                    Log("Error during phase 3: Remote Guest has reported a command execution failure.");
                    string xmlErrorLog = BuildHostXmlError("Error during phase 3: Remote Guest has reported a command execution failure.",e,_todoJob,_mainJob,_vmName,followedPath);
                    Log("Inserting done job to the DB");
                    _dbm.InsertDoneWithError(followedPath, _mainJob, xmlErrorLog);
                    Log("Deleting similar branches from db");
                    _dbm.DeleteTodoJobsStartingWithPath(followedPath);
                    Log("Freeing resources");
                    _sWriter.Close();
                    _sReader.Close();
                    _socket.Close();
                    _dbm.Dispose();
                    Log("Reverting VM");
                    //_fileLogger.Close();
                    VMManager.Instance.RevertVm(_vmName);
                    return;
                    #endregion
                }
                catch (LoopException e)
                {
                    #region Error Handling
                    Log("Error during phase 3: Loop detected.");
                    string xmlErrorLog = BuildHostXmlError("Error during phase 3: Loop detected.",e,_todoJob,_mainJob,_vmName,followedPath);
                    Log("Inserting done job to the DB");
                    _dbm.InsertDoneWithError(followedPath, _mainJob, xmlErrorLog);
                    Log("Deleting similar branches from db");
                    _dbm.DeleteTodoJobsStartingWithPath(followedPath);
                    Log("Freeing resources");
                    _sWriter.Close();
                    _sReader.Close();
                    _socket.Close();
                    _dbm.Dispose();
                    Stats.Instance.notifyLoop();
                    Log("Reverting VM");
                    //_fileLogger.Close();
                    VMManager.Instance.RevertVm(_vmName);
                    Log("Returning.");
                    return;
                    #endregion
                }
                catch (Exception e)
                {
                    #region Error Handling
                    Log("Unhandled Exception during phase 3. \n"+e.Message+"\n"+e.StackTrace);
                    string xmlErrorLog = BuildHostXmlError("Unhandled Exception during phase 3.",e,_todoJob,_mainJob,_vmName,followedPath);
                    Log("Inserting done job to the DB");
                    _dbm.InsertDoneWithError(followedPath, _mainJob, xmlErrorLog);
                    Log("Deleting similar branches from db");
                    _dbm.DeleteTodoJobsStartingWithPath(followedPath);
                    Log("Freeing resources");
                    _sWriter.Close();
                    _sReader.Close();
                    _socket.Close();
                    _dbm.Dispose();
                    Log("Reverting VM");
                    //_fileLogger.Close();
                    VMManager.Instance.RevertVm(_vmName);
                    return;
                    #endregion
                }

                
                /*------------------------------------------------------------
                 * 
                 *                          PHASE 4
                 *     Collet info about the program which has just exited
                 * 
                 * -----------------------------------------------------------
                 */
                try
                {
                    #region Phase 4: collect info about remote guest
                    Log("Remote Guest has exited. Collecting information.");
                    Log("Receiving xml info...");
                    XmlDocument xmlInfo = ReceiveMachineInfo();
                    XmlElement uisteps = xmlInfo.CreateElement("UISteps");
                    for (int i = 0; i < _followedPathBitmaps.Count; i++)
                    {
                        XmlElement el = xmlInfo.CreateElement("STEP");
                        el.SetAttribute("ID", "" + i);
                        el.InnerText = _followedPathBitmaps[i];
                        uisteps.AppendChild(el);
                    }
                    xmlInfo.GetElementsByTagName("ROOT")[0].AppendChild(uisteps);

                    // Create the file on the REPORTS folder
                    Log("Saving report...");
                    string reportPath = SaveReport(xmlInfo, _mainJob, _todoJob);
                    Log("Updating db...");
                    _dbm.InsertDone(followedPath, _mainJob, reportPath);
                    _dbm.DeleteTodoJob(_todoJob);
                    Log("Rebooting guest...");
                    
                    // Read the ACK from guest: is that a VM or not?
                    Int16 ans = _sReader.ReadInt16();
                    if (ans == Protocol.ACK_REMOTE_REBOOT)
                    {
                        // It is a VM, so reboot it
                        string vmName = _sReader.ReadString();
                        // reboot moved to finally block //fixme, i'm ugly!
                    }
                    else if (ans == Protocol.ACK_LOCAL_REBOOT)
                        throw new NotSupportedException("Bare metal support isn't available yet.");
                    else
                        throw new ProtocolException("Error: i was expecting an ACK for REMTOEREBOOT or LOCALREBOOT. I received " + ans + " instead.");
                    #endregion
                }
                catch(Exception e)
                {
                    #region Error handling
                    Log("Unhandled error during phase 4.\n"+e.Message+"\n"+e.StackTrace);
                    string xmlErrorLog = BuildHostXmlError("Unhandled error during phase 4.", e, _todoJob, _mainJob, _vmName, followedPath);
                    Log("Inserting done job to the DB");
                    _dbm.InsertDoneWithError(followedPath, _mainJob, xmlErrorLog);
                    Log("Deleting similar branches from db");
                    _dbm.DeleteTodoJobsStartingWithPath(followedPath);
                    #endregion
                }
                finally
                {
                    #region Final resource cleaning
                    _sReader.Close();
                    _sWriter.Close();
                    _socket.Close();
                    Log("Cleaning resourced for this thread and exiting.");
                    //_fileLogger.Dispose();
                    _dbm.Dispose();
                    Stats.Instance.notifyCompletedPath();
                    VMManager.Instance.RevertVm(_vmName);
                    #endregion
                }

                return;
            }

            private void DrawAndStoreBitmap(byte[] sourceBitmap,int topLeftX, int topLeftY, int width, int height)
            {
                // Draw on the bitmap and store it
                ImageConverter ic = new ImageConverter();
                using (Image img = (Image)ic.ConvertFrom(sourceBitmap))
                {
                    using (Graphics g = Graphics.FromImage(img))
                    {
                        Pen p = new Pen(Color.Red, 3);
                        g.DrawRectangle(p, new Rectangle(topLeftX, topLeftY, width, height));
                        g.Dispose();
                        using (MemoryStream ms = new MemoryStream())
                        {
                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            _followedPathBitmaps.Add(Convert.ToBase64String(ms.ToArray()));
                        }
                    }
                }
            }

            /// <summary>
            /// This function will save the report on the disk.
            /// </summary>
            /// <param name="p"></param>
            /// <param name="_mainJob"></param>
            /// <returns></returns>
            private string SaveReport(XmlDocument report, Job _mainJob, TodoJob _todoJob)
            {
                // Does the base directory for this job exist? If not, create it!
                string dirPath = Path.Combine(Properties.Settings.Default.REPORT_BASE_FOLDER,_mainJob.Id.ToString());
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                string filePath = Path.Combine(dirPath, _todoJob.Id.ToString() + ".xml");
                
                XmlWriterSettings sett = new XmlWriterSettings();
                sett.CheckCharacters=true;
                sett.Encoding=Encoding.UTF8;
                sett.Indent=true;
                sett.OmitXmlDeclaration=false;
                using (XmlWriter xw = XmlWriter.Create(filePath, sett))
                {
                    report.WriteTo(xw);
                    xw.Flush();
                }
                
                return filePath;
            }


            
            private XmlDocument ReceiveMachineInfo()
            {
                try
                {
                    XmlDocument res = new XmlDocument();

                    // 1. Receive exit code
                    int exitCode = _sReader.ReadInt32();
                    // 2. Receive std err
                    string err = _sReader.ReadString();

                    // 3. Receive the whole log
                    string xmlLog = _sReader.ReadString();
                    using (XmlReader xmlReader = new XmlTextReader(new StringReader(xmlLog)))
                    {
                        res.Load(xmlReader);
                    }

                    return res;
                }
                catch (Exception e)
                {
                    Log("Error while receiving info from remote guest."+e.Message+","+e.StackTrace);
                    return null;
                }
            }

            private void SendWorkReady()
            {
                _sWriter.Write(Protocol.JOB_READY);
            }

            private void ReceiveCallForWork()
            {
                Int16 ans = _sReader.ReadInt16();
                if (ans != Protocol.JOB_POLL_MSG)
                {
                    throw new ProtocolException("I was exprecting "+Protocol.JOB_POLL_MSG+", "+ans+" received instead.");
                }
                // Receive the name of the VM
                _vmName = _sReader.ReadString();
                ConfigureLogger();
            }

            private void ConfigureLogger()
            {
                // Check the directory existance
                if (!Directory.Exists(Settings.Default.THREAD_LOGS_DIR))
                {
                    Log("Creating directory " + Settings.Default.THREAD_LOGS_DIR);
                    try
                    {
                        Directory.CreateDirectory(Settings.Default.THREAD_LOGS_DIR);
                    }
                    catch (Exception e)
                    {
                        Log("Error during directory creation. No logs will be available. " + Settings.Default.THREAD_LOGS_DIR);
                        _fileLogger = null;
                        return ;
                    }
                }
                // Try creating the log file
                string logfilePath = Path.Combine(Settings.Default.THREAD_LOGS_DIR,_vmName+"_"+DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss")+".txt");
                // Create an empty textfile
                try
                {
                    _fileLogger = File.CreateText(logfilePath);
                    ((StreamWriter)_fileLogger).AutoFlush = true;
                    IPEndPoint remote = (IPEndPoint)_socket.Client.RemoteEndPoint;
                    Log("Logging started for VM "+_vmName+" connected to IP "+remote.Address.ToString()+":"+remote.Port+" Thread ID "+System.Threading.Thread.CurrentThread.ManagedThreadId);
                }
                catch (Exception e)
                {
                    _fileLogger = null;
                    Log("Error during log file creation: " + logfilePath + ". No log file will be available.");
                    return ;
                }
                
                
            }

            private void SendNoJobs()
            {
                _sWriter.Write(NetworkProtocol.Protocol.NO_JOBS);
            }

            private void SendRebootCommand()
            {
                // 1. Send reboot command
                _sWriter.Write(Protocol.CMD_REMOTE_REBOOT);
                // 2. Read if it is vm or not
                Int16 ans = _sReader.ReadInt16();
                if (ans == Protocol.ACK_REMOTE_REBOOT)
                { 
                    // IT IS A VM
                    // It is a VM, so reboot it
                    string vmName = _sReader.ReadString();
                    VMManager.Instance.RevertVm(vmName);
                }
                else if (ans == Protocol.ACK_LOCAL_REBOOT)
                {
                    // Will does it by itself
                }
                else
                {
                    throw new ProtocolException("");
                }

            }

            private void ReceiveProcessRestartedAck()
            {
                Int16 ans = _sReader.ReadInt16();

                if (ans != Protocol.ACK_PROCESS_RESTARTED)
                {
                    throw new ProtocolException("I was expecting process restart ack. "+ans+" received instead.");
                }
            }

            private void SendRestartProcessCommand()
            {
                _sWriter.Write(Protocol.CMD_RESTART_PROCESS);
            }

            private List<string> BuildPaths(string followedPath,PossibleInteraction pi)
            {
                // Combine all the possible actions with the base followedPath
                List<string> res = new List<string>();
                for (int i = 0; i < pi.interactiveControlN; i++)
                    res.Add(followedPath + pi.ids[i] + "=" + pi.interactionId[i] + Protocol.URI_PATH_SEP);
                    
                    return res;
            }
           

            private bool SendCommand(string controlId, int intType)
            {
                // 0. Command ID
                _sWriter.Write(Protocol.CMD_UI_INTERACT);
                
                // 1. Send the command
                _sWriter.Write(controlId);
                _sWriter.Write(intType);
                // Wait for Interaction ACK
                Int16 cmdAck = _sReader.ReadInt16();

                if (cmdAck == Protocol.CMD_OK_ACK)
                { 
                    // Ok, go ahead.
                    return true;
                }
                else if (cmdAck == Protocol.CMD_NO_ACK)
                {
                    // Something wrong, rollback required
                    return false;
                }
                else
                { 
                    // Protocol Error
                    throw new ProtocolException("I was expecting CMD_OK_ACK or CMD_NO_ACK, but received "+cmdAck);
                }
                
            }

            private PossibleInteraction ReceivePossibleInteractions()
            {
                PossibleInteraction res = new PossibleInteraction();
                // 1. Receive Window Handle
                res.winHandler = _sReader.ReadInt32();

                // 2. Read the number of interactive controls
                res.interactiveControlN = _sReader.ReadInt32();

                // 3. Read controls details
                res.ids = new string[res.interactiveControlN];
                res.interactionId = new int[res.interactiveControlN];
                res.texts = new string[res.interactiveControlN];
                res.topLeftX = new double[res.interactiveControlN];
                res.topLeftY = new double[res.interactiveControlN];
                res.bottomRightX = new double[res.interactiveControlN];
                res.bottomRightY = new double[res.interactiveControlN];
                res.focused = new bool[res.interactiveControlN];

                for (int i = 0; i < res.interactiveControlN; i++)
                {
                    // Read Id
                    res.ids[i] = _sReader.ReadString();
                    // Read the interaction id
                    res.interactionId[i] = _sReader.ReadInt32();
                    // Read the text of the button
                    res.texts[i] = _sReader.ReadString();
                    // Read the bounds relative to the current window
                    res.topLeftX[i] = _sReader.ReadDouble();
                    res.topLeftY[i] = _sReader.ReadDouble();
                    res.bottomRightX[i] = _sReader.ReadDouble();
                    res.bottomRightY[i] = _sReader.ReadDouble();
                    
                    // Focus status
                    res.focused[i] = _sReader.ReadBoolean();
                }

                // 4. Read the window Hash
                res.winStatusHash = _sReader.ReadString();

                // 5. Send Window screenshot: dimension and bytes
                int bitmapLen = _sReader.ReadInt32();
                res.bitmap = _sReader.ReadBytes(bitmapLen);
                return res;
            }
            private Int16 ReceiveProcessStatus()
            {
                return _sReader.ReadInt16();
            }
            private void ReceiveAck()
            {
                Int16 ans = _sReader.ReadInt16();
                if (ans != Protocol.ACK)
                {
                    throw new ProtocolException("Error: I was expecting an ACK: error occurred.");
                }
            }
            

        }
    }

    
}
