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
        private RichTextBox _output;
        private StreamWriter _outputFileStream;
        private string _logPath;

        public string GetLogFilePath() {
            return _logPath;
        }

        public ProgramLogger(string outputFile):base()
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

        public override void Close()
        {
            base.Close();
            _outputFileStream.Close();
        }

        public override void Write(char value)
        {
            base.Write(value);
            _outputFileStream.Write(value);
            if (_output != null)
                if (_output.InvokeRequired)
                    _output.Invoke((MethodInvoker)delegate
                    {
                        _output.AppendText("" + value);
                    });
                
        }

        public void setTextBox(RichTextBox output)
        {
            _output = output;
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }

    }
}
