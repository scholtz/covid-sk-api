using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie
{
    public class ExtendSessionResponse
    {
        [JsonConverter(typeof(Helpers.CustomDateTimeConverter))]
        public DateTimeOffset ValidThru { get; set; }
    }
}
