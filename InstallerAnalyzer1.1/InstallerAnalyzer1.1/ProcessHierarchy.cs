using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace InstallerAnalyzer1_Guest
{
    public class ProcessHierarchy
    {
        private Dictionary<uint, uint> _processes;

        public ProcessHierarchy() {
            // Child - parent
            _processes = new Dictionary<uint, uint>();
        }

        public void AddProcess(uint parentPid, uint pid) {
            lock (this)
            {
                try
                {
                    _processes.Add(pid, parentPid);
                }
                catch (Exception e) {
                    //Console.Out.WriteLine("XXX Error. Pid registered twice! XXX ");
                    // Ignore it and go on
                }
            }
        }

        public XmlNode GetHierarchy(XmlDocument doc)
        {
            var history = doc.CreateElement("ProcessHistory");

            lock (this)
            {
                Dictionary<uint, ProcessNode> flatList = new Dictionary<uint, ProcessNode>();
                // Load all the children and parents into a flat list of ProcessNode
                foreach (var e in _processes)
                {
                    // Check both key and value are into the flatlist
                    ProcessNode child = null;
                    ProcessNode parent = null;

                    if (!flatList.TryGetValue(e.Key, out child))
                    {
                        child = new ProcessNode(e.Key);
                        flatList.Add(e.Key, child);
                    }
                    if (!flatList.TryGetValue(e.Value, out parent))
                    {
                        parent = new ProcessNode(e.Value);
                        flatList.Add(e.Value, parent);
                    }

                    // Build the relationship
                    child.SetParent(parent);
                }

                // At this point the roots are the nodes with parent = NULL
                foreach (var node in flatList)
                {
                    if (node.Value.Parent == null)
                    {
                        // This gui is a root node.
                        history.AppendChild(node.Value.ToXml(doc));
                    }
                }

                return history;
            }
        }
    }

    public class ProcessNode {
        private ProcessNode _parent;
        private uint _pid;
        private List<ProcessNode> _children;

        public ProcessNode(uint pid) {
            _parent = null;
            _children = new List<ProcessNode>();
            _pid = pid;
        }

        public void SetParent(ProcessNode parent) {
            // Avoid loops: look into the hierarchy for myself.
            var p = parent;
            while (p != null) {
                if (p._pid == _pid) {
                    throw new Exception("Circular loop detected!");
                }
                p = p._parent;
            }
            
            // Ok, perform the binding, but only if not already contained
            foreach (var node in parent._children) {
                if (node._pid == _pid) { 
                    // Already contained.
                    return;
                }
            }

            parent._children.Add(this);
            _parent = parent;
        }

        public ProcessNode Parent {
            get { return _parent; }
        }

        internal XmlNode ToXml(XmlNode node)
        {
            XmlDocument document = null;
            if (node is XmlDocument)
                document = node as XmlDocument;
            else
                document = node.OwnerDocument;

            var el = document.CreateElement("Process");
            el.SetAttribute("pid",""+_pid);
            if (_parent != null)
                el.SetAttribute("ppid", "" + _parent._pid);

            foreach (var c in _children) {
                el.AppendChild(c.ToXml(document));
            }
            return el;
        }
    }

}
