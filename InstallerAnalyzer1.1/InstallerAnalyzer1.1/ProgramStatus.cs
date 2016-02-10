using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace InstallerAnalyzer1_Guest
{
    public class ProgramStatus:IObservable<uint[]>
    {
        private static ProgramStatus _instance;

        public static ProgramStatus Instance {
            get {
                if (_instance == null) {
                    _instance = new ProgramStatus();
                }
                return _instance;
            }
        }

        private bool _firstDone;
        private List<uint> _monitoredPids;
        private long _logRate, _logRateVal;
        private Thread _timer = null;
        private ProgramStatus() {
            _monitoredPids = new List<uint>();
            _clients = new List<IObserver<uint[]>>();
            _firstDone = false;
            _logRateVal = 0;
            _logRate = 0;
            _timer = new Thread(new ThreadStart(UpdateLogRate));
            
        }

        public void AddPid(uint pid) {
            uint[] notify = null;
            lock (this) {
                _monitoredPids.Add(pid);
                if (!_firstDone)
                    _firstDone = true;

                notify = _monitoredPids.ToArray();
                Monitor.Pulse(this);
            }
            foreach (var c in _clients)
                c.OnNext(notify);
        }

        public void RemovePid(uint pid)
        {
            uint[] notify = null;
            lock (this)
            {
                if (_monitoredPids.Contains(pid))
                {
                    _monitoredPids.Remove(pid);
                    notify = _monitoredPids.ToArray();
                    Monitor.Pulse(this);
                }
            }
            foreach (var c in _clients)
                c.OnNext(notify);
        }

        public uint[] Pids
        {
            get {
                lock (this)
                {
                    // Only wait if the first process is not spawned
                    while (!_firstDone)
                        Monitor.Wait(this);
                    // Note that this might return an empty array if all processes are done.
                    return _monitoredPids.ToArray();
                }
            }
        }

        private List<IObserver<uint[]>> _clients;
        public IDisposable Subscribe(IObserver<uint[]> observer)
        {
            if (!_clients.Contains(observer))
                _clients.Add(observer);

            return new Unsubscriber<uint[]>(_clients, observer);
        }

        private void UpdateLogRate() { 
            // Run every second and update the lograte
            Thread.Sleep(1000);
            lock (ProgramStatus._instance) {
                _logRateVal = _logRate;
                _logRate = 0;
            }
        }
        
        public void IncLogRate()
        {
            lock (this)
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
            lock (this) {
                while (_logRateVal != 0) {
                    return Monitor.Wait(this, IDLE_TIMEOUT);
                }
            }
            return true;
        }
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
