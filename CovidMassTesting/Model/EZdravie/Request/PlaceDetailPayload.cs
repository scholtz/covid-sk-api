using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Request
{
    public class PlaceDetailPayload
    {
        [JsonProperty("drivein_id")]
        public string DriveinId { get; set; }
    }
}
