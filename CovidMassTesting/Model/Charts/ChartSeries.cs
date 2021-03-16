using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Charts
{
    public class ChartSeries
    {
        public string Name { get; set; }
        public string Type { get; set; } = "column";
        public long[] Data { get; set; }
    }
}
