using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class NetworkConf
    {
        [JsonProperty("guest_ip")]
        public string GuestIp { get; set; }

        [JsonProperty("default_gw")]
        public string DefaultGw { get; set; }

        [JsonProperty("hc_ip")]
        public string HcIp { get; set; }

        [JsonProperty("hc_port")]
        public int HcPort { get; set; }
        
    }

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
        [JsonProperty(PropertyName = "network_conf")]
        public NetworkConf NetworkConf
        {
            get;
            set;
        }
    }
}
