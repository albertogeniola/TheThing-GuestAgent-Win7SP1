using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Host
{
    public class Logger:IDisposable
    {
        private static Logger _instance;
        public static Logger Instance
        {
            get {
                if (_instance == null)
                    _instance = new Logger();
                return _instance;
            }
        }

        private string _errFN;
        StreamWriter _errFW;
        private Logger()
        {
            _errFN = "errors_"+new DateTime().ToString("yyyy_MM_dd__hh_mm_ss") + ".txt";
            _errFW = File.CreateText(_errFN);
        }

        public void logError(string message, Exception e)
        {
            if (e != null)
                _errFW.WriteLine(new DateTime().ToString("yyyy/MM/dd hh:mm:ss") + ";" + message + "; Exception message: " + e.Message + "; Exception stacktrace: " + e.StackTrace);
            else
                _errFW.WriteLine(new DateTime().ToString("yyyy/MM/dd hh:mm:ss") + ";" + message);
        }

        public void Dispose()
        {
            _errFW.Close();
        }
    }
}
