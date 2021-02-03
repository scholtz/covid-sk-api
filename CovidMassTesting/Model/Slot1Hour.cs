using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Slot 1 hour
    /// </summary>
    public class Slot1Hour
    {
        /// <summary>
        /// id
        /// </summary>
        public long SlotId { get { return Time.Ticks; } }
        /// <summary>
        /// Place id
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// Day id
        /// </summary>
        public long TestingDayId { get; set; }
        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset Time { get; set; }
        /// <summary>
        /// Time in UTC+01:00
        /// </summary>
        public DateTimeOffset TimeInCET { get { return Time.ToOffset(new TimeSpan(1, 0, 0)); } }
        /// <summary>
        /// Day id
        /// </summary>
        public long DaySlotId { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Registrations
        /// </summary>
        public int Registrations { get; set; }
    }
}
