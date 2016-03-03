using Microsoft.Win32;
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
            _regMap = new Dictionary<string, RegAccessInfo>();
            _clients = new List<IObserver<uint[]>>();
            _firstDone = false;
            _logRateVal = 0;
            _logRate = 0;
            _timer = new Thread(new ThreadStart(UpdateLogRate));
            
        }
        
        #endregion

        #region AccessedFiles: lock on _fileMap.
        private static object _filesLock = new object();
        private static object _regLock = new object();
        // This dictionary contains a set of path->FileInfo that represents the modifications
        // made by the logged program(s) to the Filesystem.
        private Dictionary<string, FileAccessInfo> _fileMap;
        private Dictionary<string, RegAccessInfo> _regMap;

        private MD5 md5 = MD5.Create(); // TODO: implement Disposable to free this

        private string calculateFileHash(string fielPath){
            string hash = null;
            try
            {
                using (var stream = new FileStream(fielPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
            catch (Exception e) { 
                // File can be already in exclusive mode. In that case we might fail here.
                // There is little we can do trying to recover, so we just skip this.
            }
            return hash;
        }

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
                    t.OriginalFuzzyHash = null;
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
                    try
                    {
                        StringBuilder sb = new StringBuilder(150);
                        NativeMethods.fuzzy_hash_filename(s, sb);
                        t.OriginalFuzzyHash = sb.ToString();
                    }
                    catch (Exception e) { 
                        // I'm not sure if this fails frequently. Log if it happens
                        t.OriginalFuzzyHash = "INVALID";
                    }
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

        public void NotifyRegistryAccess(string regPath) { 
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
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
            catch (Exception e) { 
                // This should never happen at this time, but we don't want to stuck the process if it happens.
            }
            return hash;
        }

        public String Path { get; set; }
        public bool OriginalExisted { get; set; }
        public long OriginalSize { get; set; }
        public string OriginalHash { get; set; }
        public string OriginalFuzzyHash { get; set; }
        public string FinalHash { 
            get {
                if (Path == null)
                    return null;

                if (!File.Exists(Path))
                    return null;
                else
                    return CalculateHash(Path);
            } 
        }
        public string FinalFuzzyHash { get {

            if (Path == null)
                return null;

            try
            {
                StringBuilder sb = new StringBuilder(150);
                NativeMethods.fuzzy_hash_filename(Path, sb);
                return sb.ToString();
            }
            catch (Exception e)
            {
                // I'm not sure if this fails frequently. Log if it happens
                return "INVALID";
            }
        } }
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
                    if (new_key_value.Value.ToString() != OriginalKeyValues[new_key_value.Key].ToString())
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
