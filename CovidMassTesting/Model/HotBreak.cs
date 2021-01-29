using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// We should not cancel the slots because of statistics reasons and existing registrations
    /// 
    /// This object is intended to cancel all new visitors for specific time slots
    /// </summary>
    public class PlaceLimitation
    {
        /// <summary>
        /// Break id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Place id
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// From
        /// </summary>
        public DateTimeOffset From { get; set; }
        /// <summary>
        /// Until
        /// </summary>
        public DateTimeOffset Until { get; set; }
        /// <summary>
        /// Limit
        /// </summary>
        public int HourLimit { get; set; }
    }
}
