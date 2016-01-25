using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class RequestReportWork
    {
        [JsonProperty(PropertyName="command")]
        public String Command {
            get { return "REPORT_WORK"; }
        }

        [JsonProperty(PropertyName = "mac")]
        public String Mac
        {
            get;
            set;
        }

        [JsonProperty(PropertyName = "status")]
        public String Status
        {
            get;
            set;
        }

        [JsonProperty(PropertyName = "report_bytes_len")]
        public long ReportLenInBytes
        {
            get;
            set;
        }

        [JsonProperty(PropertyName = "work_id")]
        public long WorkId
        {
            get;
            set;
        }
    }
}
