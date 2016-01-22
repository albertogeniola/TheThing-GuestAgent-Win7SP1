using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace InstallerAnalyzer1_Guest.Protocol
{
    public class ResponseGetWork
    {
        [JsonProperty("response", Required = Required.Always)]
        public String Response
        {
            get;
            set;
        }

        [JsonProperty(PropertyName = "work_id", Required = Required.AllowNull)]
        public long WorkId
        {
            get;
            set;
        }

        [JsonProperty("file_name", Required = Required.AllowNull)]
        public String FileName
        {
            get;
            set;
        }

        [JsonProperty("file_dim", Required = Required.AllowNull)]
        public long FileDim
        {
            get;
            set;
        }

        public bool isValid(out string reason) {
            if (Response.CompareTo("GET_WORK_RESP") != 0) {
                reason = "command field should be GET_WORK_RESP.";
                return false;
            }

            if (! String.IsNullOrEmpty(Path.GetDirectoryName(FileName.Trim()))) {
                reason = "The filename received was composed with directory names. Simple filename was expected.";
                return false;
            }

            reason = null;
            return true;
            
        }

    }
}
