using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace ReportViewer
{
    public partial class Form1 : Form
    {
        private string _repoPath = null;
        public Form1()
        {
            InitializeComponent();

        }

        private void openReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var res = openReportDialog.ShowDialog(this);
            if (res == System.Windows.Forms.DialogResult.OK) {
                _repoPath = openReportDialog.FileName;
                LoadReport();
            }
        }


        private void LoadReport() {
            openReportToolStripMenuItem.Enabled = false;

            XmlReaderSettings sett = new XmlReaderSettings();
            sett.CheckCharacters = false;
            using (var reader = XmlReader.Create(_repoPath,sett))
            {
                var dt = new DataTable();
                dt.Columns.Add("Sequence", typeof(int));
                dt.Columns.Add("Pid", typeof(int));
                dt.Columns.Add("Thread Id", typeof(int));
                dt.Columns.Add("Syscall Name");
                dt.Columns.Add("Result");

                XElement root = XElement.Load(reader);
                var order = 0;
                foreach( var item in root.Descendants("SyscallInvocations").Descendants()){
                    var dr = dt.Rows.Add(new object[]{
                        order,
                        int.Parse(item.Attribute("PId").Value),
                        int.Parse(item.Attribute("ThreadId").Value),
                        item.Name,
                        int.Parse(item.Attribute("Result").Value).ToString("X")
                    });
                    order++;
                }
                DataView dv = new DataView(dt);

                //dt.Rows.Add(data.ToArray());
                dataGridView1.DataSource = dv;
            }
            // Decide what to display. We want to display 
            openReportToolStripMenuItem.Enabled = true;
        }
    }
}
