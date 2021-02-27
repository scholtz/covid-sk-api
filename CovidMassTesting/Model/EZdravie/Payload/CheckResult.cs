using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Payload
{
    public class CheckResult
    {
        public int CfdId { get; set; }
        [JsonProperty("covid_19_state")]
        public string Covid19State { get; set; }
        [JsonProperty("covid_19_state_date")]
        public string Covid19StateDate { get; set; }
        public string Etoken { get; set; }
        public string PersonTrackingNumber { get; set; }
        public string State { get; set; }
        public string Triage { get; set; }

    }
}
