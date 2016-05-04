using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ResultAnalyzerUtility
{
    public partial class FileView : Form
    {
        private XmlNode _files;

        public FileView(XmlNode files)
        {
            InitializeComponent();
            _files = files;

            if (_files == null)
                return;

            // Build the tree by directory.
            foreach(XmlNode node in _files.SelectNodes("NewFiles/File[@LeftOver='True']")) {
                AddFile(node);
            }
        }

        private void AddFile(XmlNode node) {
            string path = node.Attributes["Path"].InnerText.ToLower();
            string directory = Path.GetDirectoryName(path);
            string fname = Path.GetFileName(path);

            string parentKey = "" + Path.DirectorySeparatorChar;
            TreeNode root = null;
            if (!treeView1.Nodes.ContainsKey(parentKey))
            {
                root = treeView1.Nodes.Add(parentKey, "Root");
            } else {
                root = treeView1.Nodes.Find(parentKey, false)[0];
            }

            // Let's build the file directory tree
            string[] directories = directory.Split(Path.DirectorySeparatorChar);
            TreeNode currentParent = root;
            foreach (var d in directories) {
                var result = currentParent.Nodes.Find(parentKey + d + Path.DirectorySeparatorChar, false);
                if (result == null || result.Length == 0)
                {
                    // Missing node. Add it
                    parentKey = parentKey + d + Path.DirectorySeparatorChar;
                    currentParent = currentParent.Nodes.Add(parentKey, d,"folder");
                }
                else {
                    parentKey = parentKey + d + Path.DirectorySeparatorChar;
                    currentParent = result[0];
                }
            }

            // At this point the Directory hierarchy should be set. Add the file to it
            currentParent.Nodes.Add(path, fname,"file");

        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
