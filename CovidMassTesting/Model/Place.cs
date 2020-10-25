using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class Place
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public bool IsDriveIn { get; set; }
        public bool IsWalkIn { get; set; }
        public int Registrations { get; set; }
        public int Healthy { get; set; }
        public int Sick { get; set; }
    }
}
