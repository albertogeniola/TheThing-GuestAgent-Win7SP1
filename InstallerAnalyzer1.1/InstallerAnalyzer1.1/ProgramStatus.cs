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

        private string calculateFileHash(string fielPath){
            string hash = null;
            using (var stream = new FileStream(fielPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
            }
            return hash;
        }

        // Triggered synchronously just before NtCreateFile()
        public void NotifyFileAccess(string ss) {
            string s = "";
            if (ss.StartsWith(@"\??\"))
                s = ss.Substring(4);

            lock (_filesLock)
            {
                // Check if this is the first time we get a notification for this file. If so, check if the file exists and calculate its original hash.
                // This message is handled *BEFORE* the NtCreateFile/NtOpenFile function is called. So this is a perfect moment to check
                // whether the NtCreateFile will replace an existing file or not.

                bool firstTime = true;
                if (_fileMap.ContainsKey(s))
                {
                    firstTime = false;
                }

                bool fileExists = File.Exists(s);

                // If this is the first notification and the file didn't exist yet...
                if (!fileExists && firstTime) {
                    var t = new FileAccessInfo();
                    t.Path = s;

                    // Seems to be a create file attempt.
                    t.OriginalSize = 0;
                    t.OriginalHash = null;
                    t.OriginalExisted = false;
                    _fileMap.Add(s, t);
                }
                
                // If this is the first notification buy the file already existed...
                else if (fileExists && firstTime)
                {
                    var t = new FileAccessInfo();
                    t.Path = s;

                    // Seems to be a create/append/open file attempt.
                    FileInfo info = new FileInfo(s);
                    t.OriginalSize = info.Length;
                    t.OriginalHash = calculateFileHash(s);
                    t.OriginalExisted = true;
                    _fileMap.Add(s, t);
                }
                
                // If this is another access to a file and the file exists....
                else if (fileExists && !firstTime)
                { 
                    // Definetely looks like and openfile
                    // Nothing to do. Updated methods will be calculated on the fly.
                }

                // If this is another access to a file and the file still does not exist....
                else if (!fileExists && !firstTime) { }
                {
                    // Looks like a previous attempt has gone wrong. Do nothing for now
                    // Nothing to do. Updated methods will be calculated on the fly.
                }

                return;
            }
        }

        public IEnumerable<FileAccessInfo> FileAccessLog {
            get {
                lock (_filesLock) {
                    return _fileMap.Values;
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

    public class FileAccessInfo {

        private static string CalculateHash(string filePath) {
            string hash = null;
            using(var md5 = MD5.Create())
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            return hash;
        }

        public String Path { get; set; }
        public bool OriginalExisted { get; set; }
        public long OriginalSize { get; set; }
        public string OriginalHash { get; set; }
        public String FinalHash { 
            get {
                if (Path == null)
                    return null;

                if (!File.Exists(Path))
                    return null;
                else
                    return CalculateHash(Path);
            } 
        }

        public long FinalSize {
            get
            {
                if (Path == null)
                    return 0;

                if (!File.Exists(Path))
                    return 0;
                else
                    return (new FileInfo(Path)).Length;
            } 
        }
        public bool IsNew { 
            get {
                
                if (Path == null)
                    return false;

                return !OriginalExisted && File.Exists(Path);
            } 
        }
        public bool IsModified {
            get {
                
                if (Path == null)
                    return false;

                return OriginalExisted && OriginalHash != FinalHash;
            }
        }
        public bool IsDeleted { get {
            if (Path == null)
                return false;
            return OriginalExisted && !File.Exists(Path);
        } }
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
