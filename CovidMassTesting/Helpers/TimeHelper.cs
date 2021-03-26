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
        /// Rounds to hour
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long RoundHour(this DateTimeOffset time)
        {
            return DateTimeOffset.Parse(time.ToUniversalTime().ToString("yyyy-MM-ddTHH:00:00", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Ticks;
        }
        /// <summary>
        /// Rounds to hour
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long RoundMinute(this DateTimeOffset time)
        {
            var missingMinutes = time.Minute % 5 * -1;
            return DateTimeOffset.Parse(time.ToUniversalTime().AddMinutes(missingMinutes).ToString("yyyy-MM-ddTHH:mm:00", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture).Ticks;
        }
        /// <summary>
        /// Set time to local offset
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTimeOffset ToLocalOffset(this DateTimeOffset time)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(time.ToUniversalTime());
            return time.ToOffset(offset);
        }
        /// <summary>
        /// Returns local offset
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static TimeSpan GetLocalOffset(this DateTimeOffset time)
        {
            return TimeZoneInfo.Local.GetUtcOffset(time.ToUniversalTime());
        }
    }
}
