using InstallerAnalyzer1_Guest.UIAnalysis;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace ResultAnalyzerUtility
{
    public struct ScreenInfo {
        public string clean_image;
        public string rendered_image;
        public InstallerAnalyzer1_Guest.UIAnalysis.CandidateSet cs;
    }

    public partial class Form1 : Form
    {
        private XmlDocument _reoprtXml = new XmlDocument();
        private string SCREEN_DIR = System.IO.Path.GetTempPath() + "screens";
        private string ZIP_FILE = System.IO.Path.GetTempPath() + "screens" + ".zip";
        private List<string> bulkReports = null;
        private int repCount = 0;

        private LinkedList<ScreenInfo> images = new LinkedList<ScreenInfo>();
        private LinkedListNode<ScreenInfo> node = null;

        public Form1()
        {
            InitializeComponent();
        }

        public void loadReport(string xml) {
            try
            {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();

                progressBar1.Value = 0;
                progressBar1.Style = ProgressBarStyle.Continuous;

                images.Clear();
                
                // Check the file exists
                if (!File.Exists(xml))
                {
                    throw new Exception("The file does not exist.");
                }

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.CheckCharacters = false;
                // Load the file contents into the XML document
                using (var reader = XmlReader.Create(File.OpenRead(xml),settings))
                {
                    
                    _reoprtXml.Load(reader);
                }

                // Populate the UI
                var experiment = _reoprtXml.DocumentElement.SelectSingleNode("Experiment");
                var result = _reoprtXml.DocumentElement.SelectSingleNode("Result");
                var appLog = _reoprtXml.DocumentElement.SelectSingleNode("AppLog");
                var screens = _reoprtXml.DocumentElement.SelectSingleNode("InteractionScreenshots");
                var fileAccess = _reoprtXml.SelectNodes("InstallerAnalyzerReport/Result/FileAccess")[0];
                var newFilesLeftOver = fileAccess.SelectNodes("NewFiles/File[@LeftOver='True']");

                fileBtn.Text = ("Files (" + newFilesLeftOver.Count + ")");
                fileBtn.Tag = fileAccess;

                fileName.Text = Path.GetFileName(experiment.SelectSingleNode("InstallerName").InnerText);
                jobId.Text = experiment.SelectSingleNode("JobId").InnerText;
                duration.Text = experiment.SelectSingleNode("Duration").InnerText;
                vmResult.Text = "TODO";
                
                injectorResult.Text = result.SelectSingleNode("Injector/RetCode").InnerText;
                injectorResult.ForeColor = injectorResult.Text == "0" ? Color.DarkGreen : Color.Red;

                Out.Tag = result.SelectSingleNode("Injector/StdOut").InnerText;
                err.Tag = result.SelectSingleNode("Injector/StdErr").InnerText;
                uibotLog.Tag = _reoprtXml.DocumentElement.SelectSingleNode("AppLog").InnerText;
                
                uiBot.Text = result.SelectSingleNode("UiBot/Description").InnerText;
                Color c = Color.DarkRed;
                switch (uiBot.Text) { 
                    case "Finished":
                    case "PartiallyFinished":
                        c = Color.Green;
                        break;
                    case "UIStuck":
                        c = Color.Blue;
                        break;
                    case "TimeOut":
                        c = Color.Blue;
                        break;
                }
                uiBot.BackColor = c;
                uiBot.ForeColor = Color.White;


                // New Apps
                var newAppsNode = result.SelectSingleNode("NewApplications");
                newApps.Text = newAppsNode.Attributes["count"].InnerText;
                List<String> apps = new List<string>();
                foreach (XmlNode node in newAppsNode.SelectNodes("Application")) {
                    apps.Add(node.InnerText);
                }
                newApps.Tag = apps;


                // Extract the zip file into a temporary directory
                File.WriteAllBytes(ZIP_FILE, Convert.FromBase64String(screens.InnerText));

                // Empty the previous screen dir
                if (Directory.Exists(SCREEN_DIR))
                    Directory.Delete(SCREEN_DIR, true);
                Directory.CreateDirectory(SCREEN_DIR);
                
                // Extract the images
                using (var zipfile = ZipFile.Read(ZIP_FILE))
                {
                    zipfile.ExtractAll(SCREEN_DIR);

                    // Load the images info
                    var sp = new Regex(@"^\d+\.bmp$",RegexOptions.IgnoreCase);
                    var files = Directory.GetFiles(SCREEN_DIR).Where(f => sp.IsMatch(Path.GetFileName(f))).OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)));
                    foreach (var f in files)
                    {
                        ScreenInfo si = new ScreenInfo();
                        si.rendered_image = f;

                        int seq = int.Parse(Path.GetFileNameWithoutExtension(f));

                        // Check we have the clean image as well
                        string clean = Path.Combine(SCREEN_DIR, "clean_" + seq + ".bmp");
                        if (File.Exists(clean)) {
                            si.clean_image = clean;
                        }

                        // Check we have xml data about that
                        string xmldata = Path.Combine(SCREEN_DIR, "" + seq + ".xml");
                        if (File.Exists(xmldata))
                        {
                            XmlSerializer s = new XmlSerializer(typeof(CandidateSet));
                            using(var fr = File.OpenRead(xmldata))
                                using (XmlReader r = XmlReader.Create(fr)) {
                                    si.cs = s.Deserialize(r) as CandidateSet;
                                }
                        }
                        images.AddLast(si);
                    }
                }

                File.Delete(ZIP_FILE);
                totScreens.Text = ""+images.Count;

                node = images.Last;
                UpdateScreenshot();

            }
            catch (Exception e) {
                MessageBox.Show("Error occurred: "+e.Message);
            }
        }

        private void UpdateScreenshot() {
            if (node == null)
            {
                screenN.Text = "0";
                totScreens.Text = "0";
                pictureBox1.Image = null;
                return;
            }
            
            if (pictureBox1.Image != null)
                pictureBox1.Image.Dispose();

            int i=-1;
            var e = images.GetEnumerator();
            while (e.MoveNext()) {
                i++;
                if (e.Current.Equals(node.Value))
                    break;
            }

            var item = images.ElementAt(i);

            screenN.Text = "" + (i + 1);
            totScreens.Text = "" + images.Count;
            pictureBox1.SetScreenData(item);


            progressBar1.Value = (int)(((double)(i + 1)/(double)(images.Count))*100);

        }

        private void loadReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var res = openDialog.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.OK) {
                loadReport(openDialog.FileName);
            }
        }

        private void PreviousScreen()
        {
            if (node!=null && node.Previous!=null)
                node = node.Previous;
            UpdateScreenshot();
            
        }

        private void NextScreen()
        {
            if (node!=null && node.Next != null)
                node = node.Next;
            UpdateScreenshot();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Left))
            {
                PreviousScreen();
                return true;
            }
            else if (keyData == Keys.Right) 
            {
                NextScreen();
                return true;
            }
            else if (keyData == Keys.N) {
                showNewApps();
            }
            else if (keyData == Keys.O)
            {
                showOutput();
            }
            else if (keyData == Keys.E)
            {
                showErr();
            }else if (keyData == Keys.L)
            {
                showLog();
            }
            else if (keyData == ( Keys.Control | Keys.Right))
            {
                loadNextReport();
            }
            else if (keyData == (Keys.Control | Keys.Left))
            {
                loadPrevReport();
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void loadPrevReport() { 
            if (bulkReports != null) {
                    if (repCount == 0)
                        MessageBox.Show("This is the first report in the list.");
                    else {
                        repCount--;
                        loadReport(bulkReports[repCount]);
                        
                    }
                }
        }

        private void loadNextReport()
        {
            if (bulkReports != null) {
                if (repCount == bulkReports.Count())
                    MessageBox.Show("This was the last report in the list.");
                else {
                    repCount++;
                    loadReport(bulkReports[repCount]);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PreviousScreen();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            NextScreen();
        }

        private void newApps_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            showNewApps();
        }

        private void showNewApps()
        {
            AppLister l = new AppLister((List<string>)newApps.Tag);
            l.ShowDialog();
        }

        private void Out_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            showOutput();
        }

        private void showOutput()
        {
            OutputLister l = new OutputLister((string)this.Out.Tag);
            l.ShowDialog();
        }

        private void err_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            showErr();
        }

        private void showErr()
        {
            OutputLister l = new OutputLister((string)this.err.Tag);
            l.ShowDialog();
        }



        private void uibotLog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            showLog();
        }

        private void showLog()
        {
            OutputLister l = new OutputLister((string)this.uibotLog.Tag);
            l.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Show the list of files
            FileView fv = new FileView((sender as Button).Tag as XmlNode);
            fv.Show();
        }

        private void setDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            
            if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                var reps = new List<string>();
                // Only add directories that contain the REPORT file inside

                foreach (string s in Directory.EnumerateDirectories(fbd.SelectedPath)) {
                    string rep = Path.Combine(fbd.SelectedPath, s, "report.xml");
                    if (File.Exists(rep))
                        reps.Add(rep);
                }
                repCount = 0;
                bulkReports = reps;
                if (bulkReports.Count>0)
                    loadReport(bulkReports[repCount]);
            }
        }
    }
    
}
