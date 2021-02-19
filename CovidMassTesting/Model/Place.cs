using System;
using System.Collections.Generic;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// This object holds all information about the testing place plus real time statistics about health/sick visitors
    /// </summary>
    public class Place
    {
        /// <summary>
        /// Place id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Id of the hospital or other place provider
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Place name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Registration in minute slot is rejected if more then this amount of people are registered
        /// </summary>
        public int LimitPer5MinSlot { get; set; } = 5;
        /// <summary>
        /// Registration in hour slot is rejected if more then this amount of people are registered
        /// </summary>
        public int LimitPer1HourSlot { get; set; } = 40;
        /// <summary>
        /// Description 
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// GPS lattitude
        /// </summary>
        public decimal Lat { get; set; }
        /// <summary>
        /// GPS longtitude
        /// </summary>
        public decimal Lng { get; set; }
        /// <summary>
        /// Has drive in option
        /// </summary>
        public bool IsDriveIn { get; set; }
        /// <summary>
        /// Has walk in option
        /// </summary>
        public bool IsWalkIn { get; set; }
        /// <summary>
        /// Total number of registrations
        /// </summary>
        public int Registrations { get; set; }
        /// <summary>
        /// Results of healthy results
        /// </summary>
        public int Healthy { get; set; }
        /// <summary>
        /// Results of covid positive results
        /// </summary>
        public int Sick { get; set; }
        /// <summary>
        /// Primary Picture
        /// </summary>
        public string Picture1 { get; set; }
        /// <summary>
        /// Second Picture
        /// </summary>
        public string Picture2 { get; set; }
        /// <summary>
        /// Third Picture
        /// </summary>
        public string Picture3 { get; set; }
        /// <summary>
        /// Standard opening hours
        /// </summary>
        public string OpeningHoursWorkDay { get; set; }
        /// <summary>
        /// non Standard opening hours - weekend
        /// </summary>
        public string OpeningHoursOther1 { get; set; }
        /// <summary>
        /// non Standard opening hours - holidays
        /// </summary>
        public string OpeningHoursOther2 { get; set; }

        /// <summary>
        /// If set to false, the icon on the map should be differnt with the icon where reservation is possible
        /// </summary>
        public bool? HasReservationSystem { get; set; } = true;

        /// <summary>
        /// Link to external reservation system
        /// </summary>
        public string ExternalReservationSystem { get; set; }

        /// <summary>
        /// Is it possible to register on site or does it require preregistration?
        /// 
        /// If true, place requires preregistration
        /// </summary>
        public bool? RequiresRegistration { get; set; } = false;

        /// <summary>
        /// Old testing places will not be visible
        /// </summary>
        public bool? IsVisible { get; set; } = true;
        /// <summary>
        /// Queue in minutes
        /// </summary>
        public TimeSpan? Queue { get; set; }
        /// <summary>
        /// Queue last update time
        /// </summary>
        public DateTimeOffset? QueueLastUpdate { get; set; }
        /// <summary>
        /// Queue in minutes
        /// </summary>
        public bool? ManualQueueEntry { get; set; } = true;
        /// <summary>
        /// AvailableSlotsToday
        /// </summary>
        public int? AvailableSlotsToday { get; set; }
        /// <summary>
        /// AvailableSlotsToday
        /// </summary>
        public DateTimeOffset? AvailableSlotsTodayUpdate { get; set; }
        /// <summary>
        /// Limitations
        /// </summary>
        public List<PlaceLimitation> OtherLimitations { get; set; } = new List<PlaceLimitation>();
    }
}
