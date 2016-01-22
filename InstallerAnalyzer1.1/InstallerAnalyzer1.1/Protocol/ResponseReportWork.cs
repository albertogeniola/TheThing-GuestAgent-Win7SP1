using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class ResponseReportWork
    {
        [JsonProperty("response", Required = Required.Always)]
        public readonly String Response
        {
            get;
        }

        public bool isValid(out string reason) {
            if (Response.CompareTo("REPORT_WORK_RESP") != 0) {
                reason = "command field should be REPORT_WORK_RESP.";
                return false;
            }

            reason = null;
            return true;
            
        }

    }
}
