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

        private List<uint> _monitoredPids;
        private ProgramStatus() {
            _monitoredPids = new List<uint>();
            _clients = new List<IObserver<uint[]>>();
        }

        public void AddPid(uint pid) {
            uint[] notify = null;
            lock (this) {
                _monitoredPids.Add(pid);
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

        public uint[] GetOrWait() { 
            lock (this) {
                while (_monitoredPids.Count<1)
                    Monitor.Wait(this);
                return _monitoredPids.ToArray();
            }
        }

        public uint[] Pids
        {
            get {
                lock (this) {
                    return _monitoredPids.ToArray();
                }
            }
        }

        private List<IObserver<uint[]>> _clients;
        public IDisposable Subscribe(IObserver<uint[]> observer)
        {
            if (!_clients.Contains(observer))
                _clients.Add(observer);

            observer.OnNext(Pids.ToArray());

            return new Unsubscriber<uint[]>(_clients, observer);
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
