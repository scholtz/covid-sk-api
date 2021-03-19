using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// Time helpers 
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// Rounds to day
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long RoundDay(this DateTimeOffset time)
        {
            return DateTimeOffset.Parse(time.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Ticks;
        }
        /// <summary>
        /// Set time to local offset
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTimeOffset ToLocalOffset(this DateTimeOffset time)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            return time.ToOffset(offset);
        }

    }
}
