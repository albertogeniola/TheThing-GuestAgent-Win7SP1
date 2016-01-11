using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Host
{
    public class InconsistentPathException : Exception
    {
        public InconsistentPathException(string msg)
            : base(msg)
        { }
    }
}
