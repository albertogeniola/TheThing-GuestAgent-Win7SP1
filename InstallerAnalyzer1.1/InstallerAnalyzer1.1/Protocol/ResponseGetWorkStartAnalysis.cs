using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class ResponseGetWorkStartAnalysis
    {
        [JsonProperty(PropertyName="command")]
        public String Command {
            get { return "GET_WORK_START"; }
        }
    }
}
