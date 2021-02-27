using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Request
{
    public class ExtendSessionRequest
    {
        [JsonProperty("access_id")]
        public string AccessId { get; set; }
        [JsonProperty("user_id")]
        public int UserId { get; set; }
    }
}
