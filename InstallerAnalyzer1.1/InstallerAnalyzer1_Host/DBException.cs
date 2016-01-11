using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Host
{
    class DBException : Exception
    {
        public DBException(string msg)
            : base(msg)
        { }
    }
}
