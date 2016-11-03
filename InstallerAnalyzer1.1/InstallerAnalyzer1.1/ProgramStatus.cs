using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace InstallerAnalyzer1_Guest
{
    public class ProgramStatus:IObservable<MonitoredProcesses>
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
            _servicePids = new List<uint>();
            _hierarchy = new ProcessHierarchy();
            _fileMap = new Dictionary<string, FileAccessInfo>();
            _regMap = new Dictionary<string, RegAccessInfo>();
            _clients = new List<IObserver<MonitoredProcesses>>();
            _firstDone = false;
            _logRateVal = 0;
            _logRate = 0;
            _timer = new Thread(new ThreadStart(UpdateLogRate));
            _timer.Start();
            
        }
        
        #endregion

        #region AccessedFiles: lock on _fileMap.
        private static object _filesLock = new object();
        private static object _regLock = new object();
        // This dictionary contains a set of path->FileInfo that represents the modifications
        // made by the logged program(s) to the Filesystem.
        private Dictionary<string, FileAccessInfo> _fileMap;
        private Dictionary<string, RegAccessInfo> _regMap;
        
        public void NotifyFileRename(string oPath, string nPath)
        {
            ProgramStatus.Instance.IncFileAccessRate();
            string oldPath = null;
            string newPath = null;
            FileAccessInfo t = null;

            lock (_filesLock)
            {
                if (String.IsNullOrEmpty(oPath))
                    return;
                if (String.IsNullOrEmpty(nPath))
                    return;

                // Purge the file names
                if (oPath.StartsWith(@"\??\"))
                    oldPath = oPath.Substring(4);
                else
                    oldPath = oPath;

                if (nPath.StartsWith(@"\??\"))
                    newPath = nPath.Substring(4);
                else
                    newPath = nPath;

                // TODO: wildcard? Can they appear here?
                bool knownFile = _fileMap.ContainsKey(oldPath);

                try
                {
                    // Get the file log associated to that path or create a new one.
                    if (knownFile)
                    {
                        t = _fileMap[oldPath];

                        // At this point it is necessary to update the mapping dictionary
                        _fileMap.Remove(oldPath);
                        if (_fileMap.ContainsKey(newPath))
                            // Swap it.
                            _fileMap[newPath] = t;
                        else
                            _fileMap.Add(newPath, t);
                    }
                    else
                    {
                        t = new FileAccessInfo();
                        t.Path = oPath;
                        _fileMap.Add(oldPath, t);
                    }
                }
                catch (Exception e) {
                    throw e;
                }
            } // Release the lock on collection

            // Let the object register the file access. This will acquire the lock
            t.NotifyRenamedTo(newPath);
            
        }
        
        public void NotifyFileAccess(string ss) {
            ProgramStatus.Instance.IncFileAccessRate();
            string wild_path = "";
            if (ss.StartsWith(@"\??\"))
                wild_path = ss.Substring(4);

            var toNotify = new List<FileAccessInfo>();

            // Lock the collection
            lock (_filesLock)
            {
                string[] files = null;

                // File path may contain wildcard. It is necessary to expand them here and perform analysis on each file.
                // This, of course, KILLs performance, but gives to us much more interesting data.
                if (wild_path.Contains('*'))
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(wild_path);
                        var fname = Path.GetFileName(wild_path);
                        if (Directory.Exists(dir))
                        {
                            // Try to expand the wildcard, if any
                            files = System.IO.Directory.GetFiles(dir, fname);
                        }
                    }
                    catch (Exception e)
                    {
                        // An error occurred. Just take into account the root file
                        files = new String[] { wild_path };
                    }
                }
                else
                { // If the file does not contain any wildcard, just add it.
                    files = new String[] { wild_path };
                }


                if (files == null)
                {
                    return;
                }

                foreach (var s in files)
                {
                    if (String.IsNullOrEmpty(s))
                        continue;

                    bool knownFile = _fileMap.ContainsKey(s);
                    FileAccessInfo t = null;

                    // Get the file log associated to that path or create a new one.
                    if (knownFile)
                        t = _fileMap[s];
                    else
                    {
                        t = new FileAccessInfo();
                        t.Path = s;
                        _fileMap.Add(s, t);
                    }

                    toNotify.Add(t);

                }
            } // Release the lock on the collection

            // Let the object register the file access. This will acquire the lock on each file and eventually will require some time
            foreach(var t in toNotify)
                t.NotifyAccess();

            return;
            
        }
        
        public void NotifyRegistryAccess(string regPath) {
            ProgramStatus.Instance.IncRegAccessRate();
            lock (_regLock)
            {
                var path = regPath.ToLower();

                // Every registry key should start with "\\REGISTRY"" prefix. Catch only them.
                if (!path.StartsWith(@"\registry"))
                    return;
                
                // Check if this is the first time we get a notification for this registry key. If so, check if it exists and save its original value.
                // This message is handled *BEFORE* the NtCreateKey/NtOpenKey function is called.
                bool firstTime = true;
                
                if (_regMap.ContainsKey(path))
                {
                    firstTime = false;
                }

                string fullname = RegAccessInfo.ConvertToFullName(regPath);
                if (fullname == null) { 
                    // This was invalid. Skip.
                    return;
                }

                bool keyExists = RegAccessInfo.KeyExists(fullname);

                // If this is the first notification and the key didn't exist yet...
                if (!keyExists && firstTime)
                {
                    var t = new RegAccessInfo(fullname);
                    
                    // Seems to be a create key attempt.
                    t.PopulateOriginalValues();
                    _regMap.Add(path, t);
                }

                // If this is the first notification buy the key already existed...
                else if (keyExists && firstTime)
                {
                    var t = new RegAccessInfo(fullname);
                    
                    // Seems to be a create/append/open key attempt.
                    t.PopulateOriginalValues();
                    t.OriginalExisted = true;
                    _regMap.Add(path, t);
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

        public IEnumerable<RegAccessInfo> RegAccessLog {
            get {
                lock (_regLock) {
                    return _regMap.Values;
                }
            }
        }

        #endregion

        #region Pids and lograte. Lock on _pidsLock.
        private static object _pidsLock = new object();
        private List<uint> _monitoredPids;
        private List<uint> _servicePids;
        private int _logRate, _logRateVal;
        private bool _firstDone;
        private Thread _timer = null;
        private ProcessHierarchy _hierarchy;

        public void NotifyInjectorExited() {
            lock (_pidsLock)
            {
                // If the injector failed, we should release any lock waiting for FIRST PID.
                _firstDone = true;
                Monitor.PulseAll(_pidsLock);
            }
        }

        public ProcessHierarchy ProcHierarchy {
            get { return _hierarchy; }
        }

        public void AddPid(uint ppid, uint pid) {
            // Sometimes it happens we receive IEXplorer processes or Notepad ones.
            // This usually happens when the installer has done and displays a Thank you message
            // or similar the changelog/readme. In order to speed up the process, we should kill them.
            try
            {
                Process p = Process.GetProcessById((int)pid);
                if (p.ProcessName == "iexplore" || p.ProcessName == "notepad")
                {
                    p.Kill();
                }
            }
            catch (Exception e) {
                // Ignore this
            }

            // We might receive a process running as user or a service.
            // We will keep monitoring only user processes, not services.
            bool isService = IsService(pid);

            MonitoredProcesses msg = new MonitoredProcesses();

            lock (_pidsLock)
            {
                if (isService && !_servicePids.Contains(pid))
                    _servicePids.Add(pid);

                if (!isService && !_monitoredPids.Contains(pid))
                    _monitoredPids.Add(pid);

                _hierarchy.AddProcess(ppid, pid);
                if (!_firstDone)
                    _firstDone = true;

                msg.processPids = _monitoredPids.ToArray();
                msg.servicePids = _servicePids.ToArray();

                Monitor.Pulse(_pidsLock);
            }

            foreach (var c in _clients)
                c.OnNext(msg);
        }

        public static bool IsService(uint pid)
        {
            try
            {
                // Assume everything running in session 0 is a service
                Process p = Process.GetProcessById((int)pid);
                if (p.SessionId == 0)
                    return true;
            }
            catch (Exception e) {
                return false;
            }
            return false;
        }

        public void RemovePid(uint pid)
        {
            MonitoredProcesses msg = new MonitoredProcesses();
            
            lock (_pidsLock)
            {
                if (_monitoredPids.Contains(pid))
                    _monitoredPids.Remove(pid);

                
                if (_servicePids.Contains(pid))
                    _servicePids.Remove(pid);

                msg.processPids = _monitoredPids.ToArray();
                msg.servicePids = _servicePids.ToArray();
                Monitor.Pulse(_pidsLock);
            }

            foreach (var c in _clients)
                c.OnNext(msg);
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
            while (true)
            {
                // Run every second and update the lograte
                Thread.Sleep(1000);
                _logRateVal = Interlocked.Exchange(ref _logRate, 0);
                _fileAccessAvg = Interlocked.Exchange(ref _fileAccessCounter,0);
                _regAccessAvg = Interlocked.Exchange(ref _regAccessCounter, 0);

            }
        }
        
        public void IncLogRate()
        {
            Interlocked.Increment(ref _logRate);
        }

        float _fileAccessAvg = 0;
        int _fileAccessCounter = 0;
        private void IncFileAccessRate()
        {
            Interlocked.Increment(ref _fileAccessCounter);
        }

        float _regAccessAvg = 0;
        int _regAccessCounter = 0;
        private void IncRegAccessRate()
        {
            Interlocked.Increment(ref _regAccessCounter);
        }

        public int LogsPerSec
        {
            get
            {
                return _logRateVal;    
            }
        }

        public float FileAccessCounter
        {
            get
            {
                return _fileAccessAvg;
            }
        }

        public float RegAccessCounter
        {
            get {
                return _regAccessAvg;
            }
        }

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
        
        private List<IObserver<MonitoredProcesses>> _clients;
        
        public IDisposable Subscribe(IObserver<MonitoredProcesses> observer)
        {
            if (!_clients.Contains(observer))
                _clients.Add(observer);

            return new Unsubscriber<MonitoredProcesses>(_clients, observer);
        }
        
        #endregion

        private static object _busyLock = new object();
        private int _busy = 0;
        public void IncBusy() {
            lock (_busyLock) {
                _busy++;
            }
        }

        public void DecBusy()
        {
            lock (_busyLock)
            {
                if (_busy>0)
                    _busy--;
                Monitor.PulseAll(_busyLock);
            }
        }

        /// <summary>
        /// Blocks until there is a low logging rate and a no activity on background for at least STABILITY_DELAY seconds, or until
        /// timeout is hit. The return value indicates if the window is stable or not. In case timeout is reached while waiting for stability,
        /// false is returned.
        /// </summary>
        /// <param name="log_per_sec_threeshold"></param>
        /// <param name="timeout"></param>
        /// <returns>A boolean value indicating if the window is stable.</returns>
        public bool WaitUntilNotBusy(int log_per_sec_threeshold, int stability_delay, int timeout) {
            DateTime t = DateTime.Now;

            TimeSpan t1 = new TimeSpan(stability_delay * 10 * 1000 * 1000);
            DateTime stability_to = t.Add(t1);

            TimeSpan timer = new TimeSpan(timeout * 10 * 1000 * 1000);
            DateTime to_deadline = t.Add(timer);
            
            // We should ensure that both busy and log_per_sec remain low for enough time. 
            // We'll use a polling-approach and gain a lock at a time, avoiding possible deadlocks
            bool stable = false;
            while (true) {
                // Check if we got the timeout. In that case, break.
                DateTime now = DateTime.Now;
                if (now > to_deadline)
                    break;

                // Check stability current status. This operation acquires 2 locks, but 1 at a time, separately. There are chances
                // busy comes true while waiting the other flag, but that race condition is fine for us.
                bool busy = IsBusy() || LogsPerSec>log_per_sec_threeshold;

                // Now update timers.
                if (!busy) {
                    // System seems stable.  Update the flag and 
                    stable = true;

                    // If we have been stable for stability delay, it is time to break!
                    if (now > stability_to)
                        break;

                } else {
                    // We got a busy flag, reset the delay timer and put stability to false.
                    stable = false;
                    stability_to = now.Add(t1);
                }

                // Sleep for a while
                Thread.Sleep(100);
            }

            return stable;
        }

        public bool IsBusy() {
            lock (_busyLock) {
                return _busy>0;
            }
        }
    }

    #region Helper classes
    public struct AccessInfo
    {
        public bool exists;
        public long size;
        public Utils.Hashes hashes;
        public string fuzzyHash;
        public string path;
    }

    public class FileAccessInfo {

        private List<AccessInfo> _info = new List<AccessInfo>();
        
        // This flag is used to detect whether the file analysis has been already performed.
        // Indeed, when anyone of the callers calls "checkout()", this flag is raised and the 
        // history is frozen.
        private bool _finalized = false;
        public String Path { get; set; }

        public void NotifyAccess()
        {
            try
            {
                ProgramStatus.Instance.IncBusy();
                lock (this)
                {
                    if (_finalized)
                        // Skip
                        return;

                    // Collect info on a new struct
                    AccessInfo info = new AccessInfo();
                    info.path = Path;
                    info.exists = File.Exists(Path);

                    // If the file exists, collect more information about that
                    if (info.exists)
                    {
                        FileInfo finfo = new FileInfo(Path);
                        info.size = finfo.Length;
                        info.hashes = Utils.CalculateHash(Path);
                        info.fuzzyHash = CalculateFuzzyHash(Path);
                    }

                    // Since a new access has been performed, we might want to save this into the history.
                    // A new history line is relevant if something has changed into the file from last version.
                    // So compare the struct with the last one in memory and add it to the track if it is different.
                    if (_info.Count == 0)
                        // If this is the first time we see the file, it is necessary to add it!
                        _info.Add(info);
                    else
                    {
                        var lastinfo = _info.Last();
                        if (lastinfo.exists != info.exists ||
                            lastinfo.fuzzyHash != info.fuzzyHash ||
                            lastinfo.hashes.md5 != info.hashes.md5 ||
                            lastinfo.hashes.sha1 != info.hashes.sha1 ||
                            lastinfo.size != info.size ||
                            lastinfo.path != info.path)
                            // Something has changed, add this item to the history chain
                            _info.Add(info);
                    }
                }
            }
            finally
            {
                ProgramStatus.Instance.DecBusy();
            }    
        }

        public IEnumerable<AccessInfo> CheckoutHistory()
        {
            // Check any new change and freeze the history of the file.
            NotifyAccess();
            _finalized = true;
            return _info;
        }

        /// <summary>
        /// Calculates if the file was new to the current FS or if it was already present.
        /// </summary>
        /// <returns></returns>
        public bool IsNew() {
            if (_info.Count > 0)
                return !_info[0].exists;
            else
                return false;
        }

        /// <summary>
        /// Return true if the file existed on its first version on the FS and if the latest version 
        /// differs from the original one
        /// </summary>
        /// <returns></returns>
        public bool IsModified()
        {
            if (_info.Count > 0)
                return _info[0].exists && _info[0].hashes.sha1!= _info.Last().hashes.sha1;
            else
                return false;
        }

        private static string CalculateFuzzyHash(string filePath) {
            string res = null;
            try
            {
                StringBuilder sb = new StringBuilder(150);
                NativeMethods.fuzzy_hash_filename(filePath, sb);
                res = sb.ToString();
            }
            catch (Exception e)
            {
                // I'm not sure if this fails frequently. Log if it happens
                res = "INVALID";
            }
            return res;
        }
        
        /// <summary>
        /// Returns true if the file existed originally on the FS and if it is no more on it.
        /// </summary>
        /// <returns></returns>
        public bool IsDeleted()
        {
            if (_info.Count > 0)
                return _info[0].exists && !_info.Last().exists;
            else
                return false;
        }

        /// <summary>
        /// Returns true if the file is new and survived the installation process. This means
        /// it will be resident on the FS.
        /// </summary>
        /// <returns></returns>
        public bool LeftOver()
        {
            if (_info.Count > 0)
                return !_info[0].exists && _info.Last().exists;
            else
                return false;
        }

        public void NotifyRenamedTo(string nPath)
        {
            Path = nPath;
            NotifyAccess();
        }
    }
    
    public class RegAccessInfo
    {
        private string _fullName;
        private string _root;
        public RegAccessInfo(string fullName) {
            // We do not accept null path
            if (fullName == null)
                throw new ApplicationException("FullName cannot be null");

            _fullName = fullName;
            
            // Precalculate other define values, such as FullName, root, etc.
            var parts = FullName.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length<1) 
                throw new ApplicationException("The FullName of the registry key is invalid. ");

            _root = parts[0].ToUpper();
            if (_root != "HKEY_CURRENT_USER" &&
                _root != "HKEY_LOCAL_MACHINE" &&
                _root != "HKEY_CLASSES_ROOT" &&
                _root != "HKEY_USERS" &&
                _root != "HKEY_PERFORMANCE_DATA" &&
                _root != "HKEY_CURRENT_CONFIG" &&
                _root != "HKEY_DYN_DATA")
            {
                throw new ApplicationException("Invalid ROOT KEY specified.");
            }

        }

        public string FullName { 
            get { 
                return _fullName; 
            } 
        }

        public string Root
        {
            get
            {
                return _root;
            }
        }

        public string SubkeyPath {
            get{
                if (FullName == null)
                    throw new ArgumentException("Cannot get subkey of a null key");

                return FullName.ToLower().Split(new string[] { Root.ToLower() + "\\" }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        public RegistryKey GetRootKey() { 
            if (FullName == null)
                throw new ApplicationException("FullName cannot be null!");

            if (_root == "HKEY_CURRENT_USER")
                return Registry.CurrentUser;
            else if (_root == "HKEY_LOCAL_MACHINE")
                return Registry.LocalMachine;
            else if (_root == "HKEY_CLASSES_ROOT")
                return Registry.ClassesRoot;
            else if (_root == "HKEY_USERS")
                return Registry.Users;
            else if (_root == "HKEY_PERFORMANCE_DATA")
                return Registry.PerformanceData;
            else if (_root == "HKEY_CURRENT_CONFIG")
                return Registry.CurrentConfig;
            else if (_root == "HKEY_DYN_DATA")
                return Registry.CurrentConfig;
            else
                throw new ArgumentException("Invalid ROOT KEY specified.");
        }
        
        public bool OriginalExisted { get; set; }

        public static string ConvertToFullName(string regPath)
        {
            string path = regPath.ToLower();
            if (!path.StartsWith(@"\registry"))
                return null;
            else
                path = path.Substring(@"\registry".Length);

            // Check parts of the key
            var parts = path.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
                return null;

            string root = null;

            switch (parts[0])
            {
                case "user":
                    root = "hkey_current_user";
                    break;
                case "machine":
                    root = "hkey_local_machine";
                    break;
                case "root":
                    root = "hkey_classes_root";
                    break;
                case "users":
                    root = "hkey_users";
                    break;
                default:
                    return null;
            }

            StringBuilder result = new StringBuilder();
            result.Append(root);
            for (int i = 1; i < parts.Length; i++) {
                result.Append(@"\");
                result.Append(parts[i]);
            }
            return result.ToString();

        }



        public static bool KeyExists(string fullPath) {
            // Make it lowercase
            var p = fullPath.ToLower();
            
            // Check parts of the key
            var parts = fullPath.Split(new string[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
            RegistryKey root = null;

            switch (parts[0]) { 
                case "hkey_current_user":
                    root = Registry.CurrentUser;
                    break;
                case "hkey_local_machine":
                    root = Registry.LocalMachine;
                    break;
                case "hkey_classes_root":
                    root = Registry.ClassesRoot;
                    break;
                case "hkey_users":
                    root = Registry.Users;
                    break;
                case "hkey_performance_data":
                    root = Registry.PerformanceData;
                    break;
                case "hkey_current_config":
                    root = Registry.CurrentConfig;
                    break;
                case "hkey_dyn_data":
                    root = Registry.DynData;
                    break;
                default:
                    return false;
            }

            StringBuilder b = new StringBuilder();
            for (int i = 1; i < parts.Length; i++) {
                
                b.Append(parts[i]);

                if (i<(parts.Length-1))
                    b.Append(@"\");
            }

            var key = root.OpenSubKey(b.ToString());
            var existed = key != null;

            if (existed)
            {
                key.Dispose();
            }            
            root.Dispose();

            return existed;
        }

        public bool IsNew
        {
            get {
                if (FullName == null)
                    throw new ArgumentException("FullName cannot be null");
                
                // A key is new if didn't exist and now exists. The GetValue() returns null if the key does not exist. 
                // This is a simple trick to check if a key exists by its full path.
                return !OriginalExisted && Registry.GetValue(FullName,"test","test") != null;
            }
        }
        public bool IsModified
        {
            //todo
            get {
                if (FullName == null)
                    throw new ArgumentException("FullName cannot be null");

                return UpdateDifferences();
            }
        }


        public List<string> NewSubeys;
        public List<string> DeletedSubkeys;
        public Dictionary<string, object> DeletedValues;
        public Dictionary<string, object> NewValues;
        public Dictionary<string, object> ModifiedValues;
        public bool UpdateDifferences() {
            // Obtain actual status
            Dictionary<string, object> new_key_values = GetCurrentKeyValues();
            List<string> new_subs = GetCurrentKeySubkeys();

            // Compare and save differences
            // Look for new subkeys: ones that are now present and were not present before.
            NewSubeys = new List<string>();
            foreach (var newsub in new_subs) {
                if (!OriginalSubkeys.Contains(newsub))
                    NewSubeys.Add(newsub);
            }
            // Look for deleted subkeys: ones that are now inexistent and were present before.
            DeletedSubkeys = new List<string>();
            foreach (var oldsub in OriginalSubkeys)
            {
                if (!new_subs.Contains(oldsub))
                    DeletedSubkeys.Add(oldsub);
            }

            // Look for deleted values: ones that are now inexistent and were present before.
            DeletedValues = new Dictionary<string, object>();
            foreach (var old_key_value in OriginalKeyValues)
            {
                if (!new_key_values.ContainsKey(old_key_value.Key))
                    DeletedValues.Add(old_key_value.Key, old_key_value.Value);
            }
            
            // Look for new values: ones that didn't exist at the beginning but are now present
            NewValues = new Dictionary<string, object>();
            foreach (var new_key_value in new_key_values)
            {
                if (!OriginalKeyValues.ContainsKey(new_key_value.Key))
                    NewValues.Add(new_key_value.Key, new_key_value.Value);
            }
            
            // Look for modified values: present before and now but with different values
            ModifiedValues = new Dictionary<string, object>();
            foreach (var new_key_value in new_key_values)
            {
                if (OriginalKeyValues.ContainsKey(new_key_value.Key)) { 
                    // Add it to the list only if the value has changed or if the type has changed
                    if (new_key_value.Value!=null && OriginalKeyValues[new_key_value.Key]!=null && new_key_value.Value.ToString() != OriginalKeyValues[new_key_value.Key].ToString())
                    {
                        ModifiedValues.Add(new_key_value.Key, new
                        {
                            OriginalValue = OriginalKeyValues[new_key_value.Key],
                            CurrentValue = new_key_value.Value,
                        });
                    }
                }
                    
            }

            return NewSubeys.Count > 0 || DeletedSubkeys.Count > 0 || DeletedValues.Count > 0 || NewValues.Count > 0 || ModifiedValues.Count > 0;
        }

        private Dictionary<string, object> GetCurrentKeyValues() {
            if (FullName == null)
                throw new Exception ("FullName cannot be null");

            Dictionary<string, object> res = new Dictionary<string, object>();
            using (var rootkey = GetRootKey()) {
                var subpath = SubkeyPath;
                using (var key = rootkey.OpenSubKey(subpath)) {
                    if (key != null)
                        foreach (var name in key.GetValueNames()) {
                            res.Add(name, key.GetValue(name));
                        }
                }
            }

            return res;
        }

        private List<string> GetCurrentKeySubkeys()
        {
            if (FullName == null)
                throw new ArgumentException("FullName cannot be null");

            List<string> res = new List<string>();
            using (var rootkey = GetRootKey())
            {
                var subpath = SubkeyPath;
                using (var key = rootkey.OpenSubKey(subpath))
                {
                    if (key!=null)
                        foreach (var name in key.GetSubKeyNames())
                        {
                            res.Add(name);
                        }
                }
            }

            return res;
        }

        public bool IsDeleted
        {
            get {
                return Registry.GetValue(FullName, "test", "test") == null && OriginalExisted;
            }
        }

        public void PopulateOriginalValues()
        {
            OriginalKeyValues = GetCurrentKeyValues();
            OriginalSubkeys = GetCurrentKeySubkeys();
        }

        public Dictionary<string, object> OriginalKeyValues { get; set; }
        public List<string> OriginalSubkeys { get; set; }

    }

    public struct MonitoredProcesses {
        public uint[] processPids;
        public uint[] servicePids;
    }

    internal class Unsubscriber<Object> : IDisposable
    {
        private List<IObserver<MonitoredProcesses>> _observers;
        private IObserver<MonitoredProcesses> _observer;

        internal Unsubscriber(List<IObserver<MonitoredProcesses>> observers, IObserver<MonitoredProcesses> observer)
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
#endregion

}
