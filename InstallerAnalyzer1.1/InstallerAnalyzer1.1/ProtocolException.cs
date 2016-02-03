using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Guest
{
    class ProtocolException : Exception
    {
        public ProtocolException(string str) : base(str) { }
    }
}
