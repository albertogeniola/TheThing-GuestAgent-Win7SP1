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
    public partial class OutputLister : Form
    {
        private string text;

        public OutputLister(string text)
        {
            InitializeComponent();
            this.text = text;
            richTextBox1.Text = text;
        }

        private void OutputLister_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Filter the text
            var filter = (sender as TextBox).Text;

            if (String.IsNullOrEmpty(filter))
            {
                richTextBox1.Text = this.text;
                return;
            }

            if (text != null) {
                richTextBox1.ResetText();
                var lines = text.Split('\n');
                foreach (var l in lines) {
                    if (l.ToLower().Contains(filter.ToLower()))
                        richTextBox1.AppendText(l+"\n");
                }
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
