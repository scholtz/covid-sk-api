using System;

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
        public long SlotId => Time.Ticks;
        /// <summary>
        /// place
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset Time { get; set; }
        /// <summary>
        /// Time in UTC+01:00
        /// </summary>
        public DateTimeOffset TimeInCET => Time.ToOffset(new TimeSpan(1, 0, 0));
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Registrations stats
        /// </summary>
        public int Registrations { get; set; }
        /// <summary>
        /// Opening hours in format 09:00-10:00
        /// </summary>
        public string OpeningHours { get; set; }
        /// <summary>
        /// Template id to place opening hours definition
        /// </summary>
        public int OpeningHoursTemplate { get; set; }
    }
}
