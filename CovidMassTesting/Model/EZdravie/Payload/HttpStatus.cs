using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Payload
{
    public class HttpStatus
    {
        [JsonProperty("httpStatusCode")]
        public int HttpStatusCode { get; set; }
        [JsonProperty("httpStatusDescription")]
        public string HttpStatusDescription { get; set; }
        [JsonProperty("sqlErrorCode")]
        public string SqlErrorCode { get; set; }
        [JsonProperty("sqlErrorDescription")]
        public string SqlErrorDescription { get; set; }
        [JsonProperty("nIdOut")]
        public string IdOut { get; set; }
        [JsonProperty("session_valid_thru")]
        public string session_valid_thru { get; set; }

    }
}
