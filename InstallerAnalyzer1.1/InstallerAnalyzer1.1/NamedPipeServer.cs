using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace InstallerAnalyzer1_Guest
{
    /// <summary>
    /// This class represents a Single-Threaded named pipe server
    /// in charge of receiving pid creation notifications from
    /// the DCOM_HOOK_DLL injected into the DCOM Launcher Service Process. 
    /// </summary>
    public class NamedPipeServer
    {
        public const uint DCOM_PROCESS_SPAWN_ACK = 1;
        public const uint DCOM_PROCESS_SPAWN_NACK = 0;
        public const string DCOM_HOOK_PIPE = "dcom_hook_pipe";
        public readonly byte[] ENCODED_ACK = BitConverter.GetBytes(DCOM_PROCESS_SPAWN_ACK);
        public readonly byte[] ENCODED_NACK = BitConverter.GetBytes(DCOM_PROCESS_SPAWN_NACK);
        
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
        private Thread _t = null;

        // The boolean type is atomic, but we need to skip compiler optimizations in order to be consistent
        volatile private bool _shouldRun = false;
        volatile private bool _running = false;
        private NamedPipeServerStream _pipeServer = null;

        /// <summary>
        /// Private constructor in order to implement the singleton pattern. Does prepare the server
        /// and needed resources, but does not start the server.
        /// </summary>
        private NamedPipeServer() {
            _pipeServer = new NamedPipeServerStream(DCOM_HOOK_PIPE, PipeDirection.InOut, 1,PipeTransmissionMode.Message,PipeOptions.None);
            _running = false;
            _shouldRun = false;
        }

        public void Start() {
            lock (this)
            {
                if (_t == null)
                {
                    _t = new Thread(new ThreadStart(Run));
                }

                _shouldRun = true;

                // No need to start it again if running.
                if (!_running) {
                    _t.Start();
                }
            }
        }

        public void Stop()
        {
            lock (this)
            {
                _shouldRun = false;

                if (_t == null)
                {
                    return;
                }

                // The WaitFor connection is blocking the thread. So we need to simualte a client
                NamedPipeClientStream pipeClient = new NamedPipeClientStream(DCOM_HOOK_PIPE);
                pipeClient.Connect();
                pipeClient.ReadMode = PipeTransmissionMode.Message;
                byte[] buff = BitConverter.GetBytes(uint.MaxValue);
                pipeClient.Write(buff, 0, buff.Length);
                pipeClient.Close();
            }

            // Wait for termination
            _t.Join();
        }

        private void Run() {
            
            // Allocate a buffer that can contain the message expected from the client. 
            // Our client will send to use simple DWORD data, and a DWORD is mapped to uint in CLR.
            byte[] buff = new byte[sizeof(uint)];
            while (_shouldRun)
            {
                _running = true;
                try
                {
                    // Wait for a client to connect
                    _pipeServer.WaitForConnection();

                    // Read the request from the client. Once the client has
                    // written to the pipe its security token will be available.

                    var res = _pipeServer.Read(buff, 0, buff.Length);


                    if (res != sizeof(uint))
                    {
                        // Incomplete message! Send NOACK
                        //TODO.
                    }

                    // Unmarshal data and get the UINT.
                    uint pid = (uint)BitConverter.ToUInt32(buff, 0);

                    if (pid == uint.MaxValue) { 
                        // This means that someone has dediced to stop. No need to anwer, just quit.
                        break;
                    }

                    // Now notify our ProgramLogger and when done, send an ACK
                    ProgramStatus.Instance.AddPid(pid);

                    // Send the ACK ( ACK = 1 )
                    _pipeServer.Write(ENCODED_ACK, 0, ENCODED_ACK.Length);

                } catch (Exception e) {
                    if (_pipeServer.IsConnected) {
                        try
                        {
                            // Send a NACK and retry
                            _pipeServer.Write(ENCODED_NACK, 0, ENCODED_NACK.Length);
                            continue;
                        }
                        catch (Exception ex)
                        { 
                            //Give up!
                        }
                    }
                }
            }

            _pipeServer.Close();
            _running = false;
        }

    }
}
