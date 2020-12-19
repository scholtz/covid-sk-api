using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// For admin overview statistics of opening hours
    /// </summary>
    public class DayTimeManagement
    {
        /// <summary>
        /// slot id
        /// </summary>
        public long SlotId { get; set; }
        /// <summary>
        /// day in iso format
        /// </summary>
        public DateTimeOffset Day { get; set; }
        /// <summary>
        /// List of used templates
        /// </summary>
        public HashSet<int> OpeningHoursTemplates { get; set; }
        /// <summary>
        /// List of opening hours in standardized format
        /// </summary>
        public HashSet<string> OpeningHours { get; set; }
        /// <summary>
        /// Number of places accounted
        /// </summary>
        public int Count { get; set; }
    }
}
