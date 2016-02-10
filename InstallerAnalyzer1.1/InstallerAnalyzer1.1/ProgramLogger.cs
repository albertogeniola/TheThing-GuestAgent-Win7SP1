using InstallerAnalyzer1_Guest.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace InstallerAnalyzer1_Guest
{
    public class ProgramLogger:System.IO.TextWriter
    {
        // Singleton pattern
        private static ProgramLogger _instance;
        public static ProgramLogger Instance
        {
            get {
                if (_instance == null)
                    _instance = new ProgramLogger(Settings.Default.ApplicationLogFile);
                return _instance;
            }
        }

        // Instance attributes
        private RichTextBox _output;
        private StreamWriter _outputFileStream;
        private string _logPath;

        private ProgramLogger(string outputFile):base()
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            try
            {
                _outputFileStream = File.CreateText(outputFile);
                _outputFileStream.AutoFlush = true;
                _logPath = outputFile;
            }
            catch (Exception e)
            {
                Console.WriteLine("Application log file was invalid. Falling back to DEFAULT.");
                _logPath = "log.txt";
                _outputFileStream = File.CreateText(_logPath); //TODO: this must be parametrized
                _outputFileStream.AutoFlush = true;
            }
            
            
        }

        public string GetLogFile() {
            return _logPath;
        }

        public void setTextBox(RichTextBox output)
        {
            _output = output;
        }

        public override void Close()
        {
            base.Close();
            _outputFileStream.Close();
        }

        public override void Write(char value)
        {
            // Base calss write()
            base.Write(value);

            // If a stream has been defined, write there too
            if (_outputFileStream != null && _outputFileStream.BaseStream!=null)
                _outputFileStream.Write(value);

            // If an outputbox has been defined, write there too.
            if (_output != null)
                if (_output.InvokeRequired)
                    _output.Invoke((MethodInvoker)delegate
                    {
                        _output.AppendText("" + value);
                    });
                else
                    _output.AppendText("" + value);
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

    }
}
