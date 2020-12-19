using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Time update action
    /// </summary>
    public class TimeUpdate
    {
        /// <summary>
        /// Type - set | delete
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Date
        /// </summary>
        public DateTimeOffset Date { get; set; }
        /// <summary>
        /// Place .. If for all places, __ALL__ is defined
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// Opening hours 1-3
        /// </summary>
        public int OpeningHoursTemplateId { get; set; }

    }
}
