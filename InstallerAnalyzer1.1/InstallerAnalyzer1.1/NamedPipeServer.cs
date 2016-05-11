using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Xml;

namespace InstallerAnalyzer1_Guest
{
    /// <summary>
    /// This class represents a Single-Threaded named pipe server
    /// in charge of receiving pid creation notifications from
    /// the DCOM_HOOK_DLL injected into the DCOM Launcher Service Process. 
    /// </summary>
    public class NamedPipeServer
    {
        private const int INBUFFSIZE = 1024;
        private const int OUTBUFFSIZE = 4;

        public const uint PIPE_ACK = 1;
        public const uint PIPE_NACK = 0;
        public const string LOG_PIPE = "wk_log_pipe";
        public const string EVENT_PIPE = "wk_event_pipe";
        public readonly byte[] ENCODED_ACK = BitConverter.GetBytes(PIPE_ACK);
        public readonly byte[] ENCODED_NACK = BitConverter.GetBytes(PIPE_NACK);

        // Process Constants
        public const string WK_PROCESS_EVENT = "ProcessEvent";
        public const string WK_PROCESS_EVENT_TYPE = "Type";
        public const string WK_PROCESS_EVENT_TYPE_SPAWNED = "Spawned";
        public const string WK_PROCESS_EVENT_TYPE_DEAD = "Dead";
        public const string WK_PROCESS_EVENT_PARENT_PID = "ParentPid";
        public const string WK_PROCESS_EVENT_PID = "Pid";

        public const string WK_FILE_EVENT = "FileEvent";
        public const string WK_FILE_EVENT_MODE = "Mode";
        public const string WK_FILE_EVENT_PATH = "Path";
        public const string WK_FILE_EVENT_OLD_PATH = "OldPath";
        public const string WK_FILE_EVENT_NEW_PATH = "NewPath";
        public const string WK_FILE_CREATED = "Create";
        public const string WK_FILE_OPENED = "Open";
        public const string WK_FILE_DELETED = "Delete";
        public const string WK_FILE_RENAMED = "Rename";

        public const string WK_REGISTRY_EVENT = "RegistryEvent";
        public const string WK_REGISTRY_EVENT_MODE = "Mode";
        public const string WK_REGISTRY_EVENT_PATH = "Path";
        public const string WK_KEY_OPENED = "Open";
        public const string WK_KEY_CREATED = "Create";
        
        

        /// <summary>
        /// Single private instance implementing the singleton pattern.
        /// </summary>
        private static NamedPipeServer _instance = null;

        /// <summary>
        /// Singleton getter
        /// </summary>
        public static NamedPipeServer Instance {
            get {
                if (_instance == null) {
                    _instance = new NamedPipeServer();
                }
                return _instance;
            }
        }

        /// <summary>
        /// This is the thread running our simple thread
        /// </summary>
        //private Thread _logT = null;
        //private Thread _eventT = null;
        private Thread[] _pipeServers = null;
        private PipeSecurity _pipeSa = null;

        // The boolean type is atomic, but we need to skip compiler optimizations in order to be consistent
        volatile private bool _shouldRun = false;

        /// <summary>
        /// Private constructor in order to implement the singleton pattern. Does prepare the server
        /// and needed resources, but does not start the server.
        /// </summary>
        private NamedPipeServer() {

            _pipeSa = new PipeSecurity();
            
            // Allow everyone to read/write from the pipe
            _pipeSa.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));

            // Allow myself to control the pipe
            _pipeSa.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().Owner, PipeAccessRights.FullControl, AccessControlType.Allow));

            _shouldRun = false;
            /*
            _logT = new Thread(new ThreadStart(RunLoggerLooper));
            _eventT = new Thread(new ThreadStart(RunEventLooper));
            */
            _pipeServers = new Thread[5];
            _pipeServers[0] = new Thread(LoggerSrv);
            _pipeServers[1] = new Thread(LoggerSrv);
            _pipeServers[2] = new Thread(LoggerSrv);
            _pipeServers[3] = new Thread(LoggerSrv);
            _pipeServers[4] = new Thread(EventSrv);
        }

        public void Start() {
            lock (this)
            {   
                _shouldRun = true;
                //_logT.Start();
                //_eventT.Start();
                foreach (Thread t in _pipeServers)
                    t.Start();
            }
        }


        private static string ProcessSingleReceivedMessage(NamedPipeServerStream namedPipeServer)
        {
            StringBuilder messageBuilder = new StringBuilder();
            char[] messageChunk = null;
            byte[] messageBuffer = new byte[2]; //UTF16 requries 2 bytes per char
            do
            {
                namedPipeServer.Read(messageBuffer, 0, messageBuffer.Length);
                messageChunk = Encoding.Unicode.GetChars(messageBuffer); // Little endian UTF16
                foreach (char c in messageChunk) {
                    if (XmlConvert.IsXmlChar(c)) {
                        messageBuilder.Append(c);
                    }
                }
            }
            while (!namedPipeServer.IsMessageComplete);
            return messageBuilder.ToString();

        }

        private void LoggerSrv() {
            // Allocate the named pipe endpoint
            var srv = new NamedPipeServerStream(LOG_PIPE, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.None, INBUFFSIZE, OUTBUFFSIZE, _pipeSa);

            while (_shouldRun)
            {
                try
                {
                    // Wait for a client to connect
                    srv.WaitForConnection();
                    ProcessLogging(srv);
                }
                catch (Exception e) {
                    Console.WriteLine("Exception in pipe logger: " + e.Message + "\n " + e.StackTrace);
                    continue;
                }
            }
            srv.Close();
        }

        private void EventSrv()
        {
            var srv = new NamedPipeServerStream(EVENT_PIPE, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.None, INBUFFSIZE, OUTBUFFSIZE, _pipeSa);

            while (_shouldRun)
            {
                try
                {
                    srv.WaitForConnection();
                    ProcessEvent(srv);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in pipe event handler: " + e.Message + "\n " + e.StackTrace);
                    continue;
                }
            }
            srv.Close();
        }

            /*
            private void RunEventLooper()
            {
                NamedPipeServerStream eventPipeServer = null;
                while (_shouldRun)
                {
                    // Allocate the named pipe endpoint
                    eventPipeServer = new NamedPipeServerStream(EVENT_PIPE, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.None, INBUFFSIZE, OUTBUFFSIZE, _pipeSa);

                    // Wait for a client to connect
                    eventPipeServer.WaitForConnection();

                    //Spawn a new thread for each request and continue waiting
                    Thread t = new Thread(ProcessEvent);
                    t.Start(eventPipeServer);
                }
                eventPipeServer.Close();
            }
            */
            void ProcessEvent(NamedPipeServerStream eventPipeServer) {

                //NamedPipeServerStream eventPipeServer = o as NamedPipeServerStream;
                try
                {
                    // Read the request from the client. Once the client has
                    // written to the pipe its security token will be available.
                    var msg = ProcessSingleReceivedMessage(eventPipeServer);

                    // The received data must be in XML format
                    XmlDocument xml_doc = new XmlDocument();
                    var xmlReaderSettings = new XmlReaderSettings { CheckCharacters = false };
                    using (var stringReader = new StringReader(msg))
                    {
                        using (var xmlReader = XmlReader.Create(stringReader, xmlReaderSettings))
                        {
                            xml_doc.Load(xmlReader);
                        }
                    }
                    var xml = xml_doc.DocumentElement;

                    //Console.WriteLine(xml.Name);

                    // Handle differently depending on the ElementName
                    switch (xml.Name)
                    {
                        case WK_PROCESS_EVENT:
                            var type = xml.Attributes[WK_PROCESS_EVENT_TYPE].InnerText;

                            if (type == WK_PROCESS_EVENT_TYPE_SPAWNED)
                            {
                                var parentPid = uint.Parse(xml.Attributes[WK_PROCESS_EVENT_PARENT_PID].InnerText);
                                var pid = uint.Parse(xml.Attributes[WK_PROCESS_EVENT_PID].InnerText);
                                ProgramStatus.Instance.AddPid(parentPid, pid);
                            }
                            else if (type == WK_PROCESS_EVENT_TYPE_DEAD)
                            {
                                var pid = uint.Parse(xml.Attributes[WK_PROCESS_EVENT_PID].InnerText);
                                ProgramStatus.Instance.RemovePid(pid);
                            }

                            break;
                        case WK_FILE_EVENT:
                            var mode = xml.Attributes[WK_FILE_EVENT_MODE].InnerText;
                            if (mode == WK_FILE_CREATED || mode == WK_FILE_OPENED || mode == WK_FILE_DELETED)
                                ProgramStatus.Instance.NotifyFileAccess(xml.Attributes[WK_FILE_EVENT_PATH].InnerText);
                            else if (mode == WK_FILE_RENAMED)
                            {
                                // Convert raw bytes received by the message pump into a conveniente struct and parse the strings
                                var oldPath = xml.Attributes[WK_FILE_EVENT_OLD_PATH].InnerText;
                                var newPath = xml.Attributes[WK_FILE_EVENT_NEW_PATH].InnerText;
                                // Now notify the file rename
                                ProgramStatus.Instance.NotifyFileRename(oldPath, newPath);
                            }
                            break;
                        case WK_REGISTRY_EVENT:
                            mode = xml.Attributes[WK_REGISTRY_EVENT_MODE].InnerText;
                            if (mode == WK_KEY_OPENED || mode == WK_KEY_CREATED)
                            {
                                var path = xml.Attributes[WK_REGISTRY_EVENT_PATH].InnerText;
                                ProgramStatus.Instance.NotifyRegistryAccess(path);
                            }
                            break;
                    }

                    // Send the ACK ( ACK = 1 )
                    eventPipeServer.Write(ENCODED_ACK, 0, ENCODED_ACK.Length);
                    eventPipeServer.Flush();
                    //eventPipeServer.WaitForPipeDrain();
                    eventPipeServer.Disconnect();

                }
                catch (Exception e)
                {
                    try
                    {
                        if (eventPipeServer.IsConnected)
                            eventPipeServer.Disconnect();
                    }
                    catch (Exception e1)
                    {
                        // GIVEUP
                    }
                }
            }
        /*
            private void RunLoggerLooper() {

                NamedPipeServerStream logPipeServer = null;
                while (_shouldRun)
                {
                    // Allocate the named pipe endpoint
                    logPipeServer = new NamedPipeServerStream(LOG_PIPE, PipeDirection.InOut, -1, PipeTransmissionMode.Message, PipeOptions.None, INBUFFSIZE, OUTBUFFSIZE, _pipeSa);

                    // Wait for a client to connect
                    logPipeServer.WaitForConnection();

                    //Spawn a new thread for each request and continue waiting
                    Thread t = new Thread(ProcessLogging);
                    t.Start(logPipeServer);
                }
                logPipeServer.Close();
            }
            */

            void ProcessLogging(NamedPipeServerStream logPipeServer) {

            //NamedPipeServerStream logPipeServer = o as NamedPipeServerStream;

            try
            {
                // Read the request from the client. Once the client has
                // written to the pipe its security token will be available.
                var log_msg = ProcessSingleReceivedMessage(logPipeServer);

                ProgramStatus.Instance.IncLogRate();
                //Console.Out.WriteLine(log_msg);
                LogSyscall(log_msg);

                // Send the ACK ( ACK = 1 )
                logPipeServer.Write(ENCODED_ACK, 0, ENCODED_ACK.Length);
                logPipeServer.Flush();
                logPipeServer.Disconnect();

            }
            catch (Exception e)
            {
                try
                {
                    if (logPipeServer.IsConnected)
                        logPipeServer.Disconnect();
                }
                catch (Exception e1)
                {
                    // GIVEUP
                }
            }
        }
        

        //private long _seq = 0;
        private void LogSyscall(string xmlIn)
        {
            string row = null;
            try
            {
                row = xmlIn.Normalize();
            }
            catch (Exception e)
            {
                row = UnicodeEncoding.Unicode.GetString(UnicodeEncoding.Unicode.GetBytes(xmlIn));
            }


            // Load the XML
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(row);
            }
            catch (Exception e)
            {
                // This kind of error may happen when we receive invalid XML. Load it in base64
                // format for further investigation
                var sc = doc.CreateElement("Syscall");
                sc.SetAttribute("Method", "UNKNOWN");
                //sc.SetAttribute("Sequence", _seq.ToString());
                doc.AppendChild(sc);

                // Encode in base 64 and add it to the raw data
                var rawData = doc.CreateElement("RawData");
                byte[] b = Encoding.UTF8.GetBytes(row);
                rawData.InnerText = Convert.ToBase64String(b);
                sc.AppendChild(rawData);

                //Interlocked.Increment(ref _seq);
                Program.appendXmlLog(sc);
                //logbox.Text = row;
                return;
            }

            XmlElement root = doc.DocumentElement;

            // Now translate into a structured form
            string syscallName = root.Name;
            var el = doc.CreateElement("Syscall");
            el.SetAttribute("Method", syscallName);
            //el.SetAttribute("Sequence", Interlocked.Read(ref _seq).ToString());
            doc.RemoveChild(root);
            doc.AppendChild(el);

            // Add the syscall name
            var method = doc.CreateElement("Method");
            method.InnerText = syscallName;
            el.AppendChild(method);

            foreach (XmlAttribute attr in root.Attributes)
            {
                var child = doc.CreateElement(attr.Name);
                child.InnerText = attr.Value;
                el.AppendChild(child);
            }

            //Interlocked.Increment(ref _seq);
            Program.appendXmlLog(el);
            //logbox.Text = row;

        }

        public void Stop()
        {
            _shouldRun = false;
        }
    }

}
