using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class RequestGetWorkFileReceived
    {
        [JsonProperty(PropertyName="command")]
        public readonly String Command {
            get { return "GET_WORK_FILE_RECEIVED"; }
        }
    }
}
