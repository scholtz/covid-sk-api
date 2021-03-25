using System;

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
        public long SlotId => Time.Ticks;
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
        /// Time in UTC+01:00
        /// </summary>
        public DateTimeOffset TimeInCET => Time.ToLocalTime();
        /// <summary>
        /// Time from ticks
        /// </summary>
        public DateTimeOffset TimeFromTicks => new DateTimeOffset(SlotId, TimeSpan.Zero);
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
        public long Registrations { get; set; }
    }
}
