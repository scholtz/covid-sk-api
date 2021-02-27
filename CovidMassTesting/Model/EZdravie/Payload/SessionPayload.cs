using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie
{
    public class SessionPayload
    {
        public int DebugMode { get; set; }
        public string SessionId { get; set; }
        public string Token { get; set; }
        [JsonConverter(typeof(Helpers.CustomDateTimeConverter))]
        public DateTimeOffset ValidThru { get; set; }
    }
}
