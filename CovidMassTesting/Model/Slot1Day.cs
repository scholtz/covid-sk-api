using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Day slot at specific place with registration stats
    /// </summary>
    public class Slot1Day
    {
        /// <summary>
        /// id
        /// </summary>
        public long SlotId { get { return Time.Ticks; } }
        /// <summary>
        /// place
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset Time { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Registrations stats
        /// </summary>
        public int Registrations { get; set; }
    }
}
