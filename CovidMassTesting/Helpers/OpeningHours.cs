using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// OpeningHours extension
    /// </summary>
    public static class OpeningHours
    {
        /// <summary>
        /// Parse opening hours
        /// 
        /// 09:00 - 10:00, 10:05 - 20:00
        /// </summary>
        /// <param name="openingHours"></param>
        /// <returns></returns>
        public static SortedDictionary<long, bool> ParseOpeningHours(this string openingHours)
        {
            var ret = new SortedDictionary<long, bool>();
            if (string.IsNullOrEmpty(openingHours))
            {
                return ret;
            }
            foreach (var singleTimeRange in openingHours.Split(","))
            {
                var range = singleTimeRange.Split("-");
                if (range.Length != 2) throw new Exception("Invalid opening hours format");
                var time1 = TimeSpan.Parse(range[0].Trim());
                var time2 = TimeSpan.Parse(range[1].Trim());
                ret[time1.Ticks] = true;
                ret[time2.Ticks] = false;
            }
            return ret;
        }
        /// <summary>
        /// Validates the opening hours format
        /// </summary>
        /// <param name="openingHours"></param>
        /// <returns></returns>
        public static bool ValidateOpeningHours(this string openingHours)
        {
            try
            {
                openingHours.ParseOpeningHours();
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Checks if time is within the range
        /// </summary>
        /// <param name="hours"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static bool IsTimeWhenIsOpen(this SortedDictionary<long, bool> hours, TimeSpan time)
        {
            if (hours is null)
            {
                throw new ArgumentNullException(nameof(hours));
            }

            var tickBefore = hours.Keys.LastOrDefault(t => t <= time.Ticks);
            if (hours.ContainsKey(tickBefore)) return hours[tickBefore];
            return false;
        }
        /// <summary>
        /// Checks if time is within the range
        /// </summary>
        /// <param name="hours"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public static bool HasAnySlotWithinHourOpen(this SortedDictionary<long, bool> hours, TimeSpan time)
        {
            if (hours is null)
            {
                throw new ArgumentNullException(nameof(hours));
            }

            var from = TimeSpan.FromHours(time.Hours);
            if (hours.IsTimeWhenIsOpen(from)) return true;
            var until = from.Add(TimeSpan.FromHours(1));
            var fromTicks = from.Ticks;
            var untilTicks = until.Ticks;
            return hours.Any(kv => kv.Key >= fromTicks && kv.Key < untilTicks && kv.Value == true);
        }
    }
}
