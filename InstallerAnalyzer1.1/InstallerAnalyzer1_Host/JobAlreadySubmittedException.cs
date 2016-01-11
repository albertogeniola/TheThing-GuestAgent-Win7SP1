using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Host
{
    class JobAlreadySubmittedException : Exception
    {
        private int _jobId;

        public int Id
        {
            get { return _jobId; }
        }

        public JobAlreadySubmittedException(int previusId)
        {
            _jobId = previusId;
        }
    }
}
