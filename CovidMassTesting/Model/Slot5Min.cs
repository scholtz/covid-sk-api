using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// 5 minute slot
    /// </summary>
    public class Slot5Min
    {
        /// <summary>
        /// Slot id
        /// </summary>
        public long SlotId { get { return Time.Ticks; } }
        /// <summary>
        /// place id
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// Day id
        /// </summary>
        public long TestingDayId { get; set; }
        /// <summary>
        /// Slot time object
        /// </summary>
        public DateTimeOffset Time { get; set; }
        /// <summary>
        /// Hour slot id
        /// </summary>
        public long HourSlotId { get; set; }
        /// <summary>
        /// Formatted time. Eg. 15:00 - 20:00
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Registrations stats
        /// </summary>
        public int Registrations { get; set; }
    }
}
