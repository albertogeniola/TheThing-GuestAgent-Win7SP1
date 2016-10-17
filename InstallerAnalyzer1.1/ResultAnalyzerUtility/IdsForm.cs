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
    public partial class IdsForm : Form
    {
        string[] results = new string[] { };

        public IdsForm()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            results = ids.Text.Split(';');
            this.DialogResult = DialogResult.OK;
            Dispose();
        }

        public string[] get_result() {
            return results;
        }
    }
}
