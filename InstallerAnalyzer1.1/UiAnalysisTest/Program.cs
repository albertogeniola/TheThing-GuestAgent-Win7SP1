using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UiAnalysisTest
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            int ok = 0;
            int fail = 0;
            var files = Directory.EnumerateFiles(path, "test?.bmp");
            foreach (string f in files) {
                var t = new Form1(f).ShowDialog();
                if (t == DialogResult.OK) ok++;
                else fail++;
            }

            MessageBox.Show("Success: " + ok + ". Failures: " + fail);

            Application.Run();
            /*
            var res = d.ShowDialog();
            if (res != DialogResult.OK)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            */
        }
    }
}
