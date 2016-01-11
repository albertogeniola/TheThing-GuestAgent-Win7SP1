using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Host
{
    public class GuestCommandFailedException : Exception
    {
        public GuestCommandFailedException(string msg)
            : base(msg)
        { }
    }
}
