using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InstallerAnalyzer1_Guest
{
    public class Job
    {
        private readonly long _id;
        public long Id { get { return _id; } }

        private readonly string _localPath;
        public string LocalFullPath { get { return _localPath; } }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public Job(long id, string localPath) {
            this._id = id;

            if (!File.Exists(localPath))
                throw new ArgumentException("File does not exist.");

            this._localPath = localPath;
        }
    }
}
