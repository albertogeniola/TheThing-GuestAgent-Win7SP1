using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Guest
{
    public class UnknownControl:Controls.Control
    {
        public UnknownControl(IntPtr handle, string className, string id, string text) : base(handle, className, id, text)
        {}
    }
}
