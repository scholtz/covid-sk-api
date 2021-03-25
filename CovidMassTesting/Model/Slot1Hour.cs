using System;

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
        public long SlotId => Time.Ticks;
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
        public DateTimeOffset TimeInCET => Time.ToLocalTime();
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
        public long Registrations { get; set; }
    }
}
