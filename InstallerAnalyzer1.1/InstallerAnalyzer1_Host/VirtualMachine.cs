using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Host
{
    public interface VirtualMachine
    {
        public static IEnumerable<VirtualMachine> Load();
        public static VirtualMachine Create();
        public void Start();
        public void Revert();
        public VMStatus GetStatus();
        
    }
}
