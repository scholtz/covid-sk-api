using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class TestingDay
    {
        public DateTimeOffset Time { get; set; }
        public int Registrations { get; set; }
        public int Healthy { get; set; }
        public int Sick { get; set; }
    }
}
