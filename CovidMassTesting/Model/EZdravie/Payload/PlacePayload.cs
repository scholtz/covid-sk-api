using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie.Payload
{
    public class PlacePayload
    {
        public string Cfdc { get; set; }
        public string City { get; set; }
        public int DailyCapacity { get; set; }
        public string Id { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string StreetName { get; set; }
        public string StreetNumber { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string ZipCode { get; set; }
    }
}
