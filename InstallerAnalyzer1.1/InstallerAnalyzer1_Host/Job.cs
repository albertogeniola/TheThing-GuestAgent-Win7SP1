using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerAnalyzer1_Host
{
    public class Job
    {
        private int _id;
        private string _filePath;
        private string _hash;
        private DateTime _insertDate;
        private DateTime? _startDate;
        private DateTime? _finishDate;
        private bool _finished;

        public Job(int id, string filePath, string hash, DateTime insertDate, DateTime? startDate, DateTime? finishDate, bool finished)
        {
            _id = id;
            _filePath = filePath;
            _hash = hash;
            _insertDate = insertDate;
            _startDate = startDate;
            _finishDate = finishDate;
            _finished = finished;
        }

        public string InstallerPath
        {
            get {
                return _filePath;
            }
        }

        public int Id
        {
            get {
                return _id;
            }
        }

    }
}
