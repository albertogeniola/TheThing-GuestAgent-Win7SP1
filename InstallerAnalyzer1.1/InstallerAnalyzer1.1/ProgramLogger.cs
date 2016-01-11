using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace InstallerAnalyzer1_Guest
{
    public class ProgramLogger:System.IO.TextWriter
    {
        private RichTextBox _output;
        private StreamWriter _outputFileStream;

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
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                Application.Exit();
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
                _output.AppendText(""+value);
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
