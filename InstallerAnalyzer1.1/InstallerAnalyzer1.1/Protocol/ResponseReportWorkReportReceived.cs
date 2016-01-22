using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class ResponseReportWorkReportReceived
    {
        [JsonProperty(PropertyName="command")]
        public String Response {
            get { return "REPORT_WORK_REPORT_RECEIVED"; }
        }

        public bool isValid(out string reason)
        {
            if (Response.CompareTo("REPORT_WORK_REPORT_RECEIVED") != 0)
            {
                reason = "command field should be REPORT_WORK_REPORT_RECEIVED.";
                return false;
            }

            reason = null;
            return true;

        }
    }
}
