using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Charts
{
    public class Dashboard
    {
        public string[] Labels { get; set; }
        public List<ChartSeries> Series { get; set; } = new List<ChartSeries>();
    }
}
