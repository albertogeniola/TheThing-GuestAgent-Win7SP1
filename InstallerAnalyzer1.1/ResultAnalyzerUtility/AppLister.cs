using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResultAnalyzerUtility
{
    public partial class AppLister : Form
    {
        public AppLister(List<string> apps)
        {
            InitializeComponent();
            listBox1.Items.Clear();
            if (apps!=null)
                foreach (string s in apps) {
                    listBox1.Items.Add(s);
                }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || keyData == Keys.Enter)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

    }
}
