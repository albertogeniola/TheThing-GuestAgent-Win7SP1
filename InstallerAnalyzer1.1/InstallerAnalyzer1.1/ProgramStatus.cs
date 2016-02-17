using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace InstallerAnalyzer1_Guest
{
    public class ProgramStatus:IObservable<uint[]>
    {
        
        #region Singleton

        // Singleton pattern
        private static ProgramStatus _instance;
        
        public static ProgramStatus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProgramStatus();
                }
                return _instance;
            }
        }
        
        private ProgramStatus() {
            _monitoredPids = new List<uint>();
            _fileMap = new Dictionary<string, FileAccessInfo>();
            _clients = new List<IObserver<uint[]>>();
            _firstDone = false;
            _logRateVal = 0;
            _logRate = 0;
            _timer = new Thread(new ThreadStart(UpdateLogRate));
            
        }
        
        #endregion

        #region AccessedFiles: lock on _fileMap.
        private static object _filesLock = new object();
        // This dictionary contains a set of path->FileInfo that represents the modifications
        // made by the logged program(s) to the Filesystem.
        private Dictionary<string, FileAccessInfo> _fileMap;

        private MD5 md5 = MD5.Create(); // TODO: implement Disposable to free this
        public void NotifyFileAccess(string ss)
        {
            string s = "";
            if (ss.StartsWith(@"\??\"))
                s = ss.Substring(4);
            lock (_filesLock)
            { 
                // Check if this is the first time we access the program. If our dictionary contains no rows
                // associated with this path, then it's the first time. 
                if (!_fileMap.ContainsKey(s)) { 
                    // First time:
                    // Create a file info and caclulate the initial path of the file. This, only if the file exists.
                    // In case of error we might skip everything.
                    if (!File.Exists(s))
                        return;

                    string hash = null;
                    try {
                        using (var stream = File.OpenRead(s))
                        {
                            hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                        }
                        FileAccessInfo t = new FileAccessInfo();
                        t.OriginalHash = hash;
                        t.OriginalSize = (new FileInfo(s)).Length;
                        t.Deleted = false;
                        t.Path = s;
                        _fileMap.Add(s, t);
                        
                        return;
                    } catch(Exception e){
                        // Ignore!
                        return;
                    }
                }
                
                // Otherwise this is a second-access. The file might be deleted or updated
                if (!File.Exists(s))
                {
                    _fileMap[s].Deleted = true;
                }
                else {
                    try
                    {
                        string hash = null;
                        using (var stream = File.OpenRead(s))
                        {
                            hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                        }
                        _fileMap[s].FinalSize = (new FileInfo(s)).Length;
                        _fileMap[s].FinalHash = hash;
                        
                        return;
                    }
                    catch (Exception e)
                    {
                        // Ignore!
                        return;
                    }
                }

            }
        }

        #endregion

        #region Pids and lograte. Lock on _pidsLock.
        private static object _pidsLock = new object();
        private List<uint> _monitoredPids;
        private long _logRate, _logRateVal;
        private bool _firstDone;
        private Thread _timer = null;
        
        public void AddPid(uint pid) {
            uint[] notify = null;
            lock (_pidsLock)
            {
                _monitoredPids.Add(pid);
                if (!_firstDone)
                    _firstDone = true;

                notify = _monitoredPids.ToArray();
                Monitor.Pulse(_pidsLock);
            }
            foreach (var c in _clients)
                c.OnNext(notify);
        }
        
        public void RemovePid(uint pid)
        {
            uint[] notify = null;
            lock (_pidsLock)
            {
                if (_monitoredPids.Contains(pid))
                {
                    _monitoredPids.Remove(pid);
                    notify = _monitoredPids.ToArray();
                    Monitor.Pulse(_pidsLock);
                }
            }
            foreach (var c in _clients)
                c.OnNext(notify);
        }
        
        public uint[] Pids
        {
            get {
                lock (_pidsLock)
                {
                    // Only wait if the first process is not spawned
                    while (!_firstDone)
                        Monitor.Wait(_pidsLock);
                    // Note that this might return an empty array if all processes are done.
                    return _monitoredPids.ToArray();
                }
            }
        }
        
        private void UpdateLogRate() { 
            // Run every second and update the lograte
            Thread.Sleep(1000);
            lock (_pidsLock)
            {
                _logRateVal = _logRate;
                _logRate = 0;
            }
        }
        
        public void IncLogRate()
        {
            lock (_pidsLock)
            {
                _logRate++;
            }
        }
        
        public long LogsPerSec { get { return _logRateVal; } }
        
        /// <summary>
        /// This method holds until the Installer is supposed to be IDLE. If timeout is reached, false is returned. Otherwise if we detect the 
        /// IDLE state before the timeout, true is returned.
        /// </summary>
        /// <param name="IDLE_TIMEOUT"></param>
        /// <returns></returns>
        public bool WaitUntilIdle(int IDLE_TIMEOUT)
        {
            // Returns false if timeout is reached
            lock (_pidsLock)
            {
                while (_logRateVal != 0) {
                    return Monitor.Wait(_pidsLock, IDLE_TIMEOUT);
                }
            }
            return true;
        }
        
        #endregion

        #region Observer implementation
        
        private List<IObserver<uint[]>> _clients;
        
        public IDisposable Subscribe(IObserver<uint[]> observer)
        {
            if (!_clients.Contains(observer))
                _clients.Add(observer);

            return new Unsubscriber<uint[]>(_clients, observer);
        }
        
        #endregion
    }

    class FileAccessInfo {
        public String Path { get; set; }
        public String OriginalHash { get; set; }
        public String FinalHash { get; set; }
        public bool Deleted { get; set; }
        public long OriginalSize { get; set; }
        public long FinalSize { get; set; }
    }

    internal class Unsubscriber<Object> : IDisposable
    {
        private List<IObserver<uint[]>> _observers;
        private IObserver<uint[]> _observer;

        internal Unsubscriber(List<IObserver<uint[]>> observers, IObserver<uint[]> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}
