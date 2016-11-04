using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace InstallerAnalyzer1_Guest
{
    public class ModuleInfo
    {
        public string Name { get; }
        public string Path { get; }
        public string Sha1 { get; }
        public string Md5 { get; }

        public ModuleInfo(string name, string path)
        {
            Name = name;
            Path = path;
            var hashes = Utils.CalculateHash(path);
            Sha1 = hashes.sha1;
            Md5 = hashes.md5;
        }

        internal XmlNode ToXml(XmlDocument document)
        {
            var module = document.CreateElement("Module");

            var sha1 = document.CreateElement("Sha1");
            sha1.InnerText = Sha1;
            module.AppendChild(sha1);

            var md5 = document.CreateElement("Md5");
            md5.InnerText = Md5;
            module.AppendChild(md5);

            var name = document.CreateElement("Name");
            name.InnerText = Name;
            module.AppendChild(name);

            var path = document.CreateElement("Path");
            path.InnerText = Path;
            module.AppendChild(path);

            Utils.DummySerialize(FileVersionInfo.GetVersionInfo(Path), module);

            return module;
        }
    }

    public class ProcessInfo
    {
        public ModuleInfo MainModule { get; }
        public IEnumerable<ModuleInfo> Modules { get; }
        public string MainWindowTitle { get; }

        private ProcessInfo(Process p)
        {
            // Get the list of modules
            try
            {
                MainWindowTitle = p.MainWindowTitle;
            }
            catch (Exception e) { }

            try
            {
                MainModule = new ModuleInfo(p.MainModule.ModuleName, p.MainModule.FileName);
            }
            catch (Exception e) { }


            var m = new List<ModuleInfo>();
            try
            {
                foreach (ProcessModule pm in p.Modules)
                {

                    m.Add(new ModuleInfo(pm.ModuleName, pm.FileName));
                }
            }
            catch (Exception e) { }

            Modules = m;
        }

        public static ProcessInfo FromPid(uint pid)
        {
            return new ProcessInfo(Process.GetProcessById((int)pid));
        }

        internal XmlNode ToXml(XmlDocument document)
        {
            var procInfo = document.CreateElement("ProcessInfo");

            var title = document.CreateElement("WindowTitle");
            title.InnerText = MainWindowTitle;
            procInfo.AppendChild(title);

            XmlNode main_module = document.CreateElement("Module");
            if (MainModule != null)
                main_module = MainModule.ToXml(document);

            procInfo.AppendChild(main_module);

            // Modules
            var inner_modules = document.CreateElement("LoadedModules");
            if (Modules != null)
            {
                foreach (var m in Modules)
                {
                    inner_modules.AppendChild(m.ToXml(document));
                }
            }
            procInfo.AppendChild(inner_modules);

            return procInfo;
        }
    }

    public class ProcessHierarchy
    {
        private Dictionary<uint, uint> _processes;
        private Dictionary<uint, ProcessInfo> _pinfo;

        public ProcessHierarchy()
        {
            // Child - parent
            _processes = new Dictionary<uint, uint>();
            _pinfo = new Dictionary<uint, ProcessInfo>();
        }

        public void AddProcess(uint parentPid, uint pid)
        {
            lock (this)
            {
                try
                {
                    _processes.Add(pid, parentPid);
                    _pinfo.Add(pid, ProcessInfo.FromPid(pid));
                }
                catch (Exception e)
                {
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
                        ProcessInfo pi = null;
                        if (!_pinfo.TryGetValue(e.Key, out pi))
                        {
                            pi = ProcessInfo.FromPid(e.Key);
                        }
                        child = new ProcessNode(e.Key, pi);
                        flatList.Add(e.Key, child);
                    }
                    if (!flatList.TryGetValue(e.Value, out parent))
                    {
                        ProcessInfo pi = null;
                        if (!_pinfo.TryGetValue(e.Value, out pi))
                        {
                            pi = ProcessInfo.FromPid(e.Value);
                        }
                        parent = new ProcessNode(e.Value, pi);
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

    public class ProcessNode
    {
        private ProcessNode _parent;
        private uint _pid;
        private List<ProcessNode> _children;
        private ProcessInfo _info;

        public ProcessNode(uint pid, ProcessInfo info)
        {
            _parent = null;
            _children = new List<ProcessNode>();
            _pid = pid;
            _info = info;
        }

        public void SetParent(ProcessNode parent)
        {
            // Avoid loops: look into the hierarchy for myself.
            var p = parent;
            while (p != null)
            {
                if (p._pid == _pid)
                {
                    throw new Exception("Circular loop detected!");
                }
                p = p._parent;
            }

            // Ok, perform the binding, but only if not already contained
            foreach (var node in parent._children)
            {
                if (node._pid == _pid)
                {
                    // Already contained.
                    return;
                }
            }

            parent._children.Add(this);
            _parent = parent;
        }

        public ProcessNode Parent
        {
            get { return _parent; }
        }

        internal XmlNode ToXml(XmlDocument document)
        {
            var el = document.CreateElement("Process");
            el.SetAttribute("pid", "" + _pid);
            if (_parent != null)
                el.SetAttribute("ppid", "" + _parent._pid);

            // Process info:
            var info = _info.ToXml(document);
            el.AppendChild(info);

            var children = document.CreateElement("Children");
            foreach (var c in _children)
            {
                children.AppendChild(c.ToXml(document));
            }
            el.AppendChild(children);
            return el;
        }
    }

}
