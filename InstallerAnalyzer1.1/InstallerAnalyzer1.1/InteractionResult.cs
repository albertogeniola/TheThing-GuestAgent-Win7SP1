using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Guest
{
    enum InteractionResult
    {
        Unknown = 0,
        Finished = 1,
        UI_Stuck = 2,
        UnknownError = 3,
        PartiallyFinished = 4,
        TimeOut
    }
}
