using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Stats types
    /// </summary>
    public class StatsType
    {
        /// <summary>
        /// Enum
        /// </summary>
        public enum Enum
        {
            /// <summary>
            /// Tested persons
            /// </summary>
            Tested,
            /// <summary>
            /// EHealth notifications
            /// </summary>
            EHealthNotification,
            /// <summary>
            /// Notifications
            /// </summary>
            Notification,
            /// <summary>
            /// Test repeated
            /// </summary>
            Repeat,
            /// <summary>
            /// Positive cases
            /// </summary>
            Positive,
            /// <summary>
            /// Negative cases
            /// </summary>
            Negative,
            /// <summary>
            /// Registered to date
            /// </summary>
            RegisteredTo,
            /// <summary>
            /// Registered on date
            /// </summary>
            RegisteredOn
        }
        /// <summary>
        /// Enum to text
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToText(StatsType.Enum type)
        {
            return type switch
            {
                StatsType.Enum.Tested => StatsType.Tested,
                StatsType.Enum.EHealthNotification => StatsType.EHealthNotification,
                StatsType.Enum.Notification => StatsType.Notification,
                StatsType.Enum.Repeat => StatsType.Repeat,
                StatsType.Enum.Positive => StatsType.Positive,
                StatsType.Enum.Negative => StatsType.Negative,
                StatsType.Enum.RegisteredTo => StatsType.RegisteredTo,
                StatsType.Enum.RegisteredOn => StatsType.RegisteredOn,
                _ => throw new Exception("Invalid slot type"),
            };
        }

        /// <summary>
        /// Number of tested persons
        /// </summary>
        public const string Tested = "tested";
        /// <summary>
        /// EHealth notifications
        /// </summary>
        public const string EHealthNotification = "ehealth-notification";
        /// <summary>
        /// Number of notifications sent
        /// </summary>
        public const string Notification = "notification";
        /// <summary>
        /// Number of test results to be repeated
        /// </summary>
        public const string Repeat = "repeat";
        /// <summary>
        /// Number of positive test results
        /// </summary>
        public const string Positive = "positive";
        /// <summary>
        /// Number of negative test results
        /// </summary>
        public const string Negative = "negative";
        /// <summary>
        /// Day for which new registrations were registered
        /// </summary>
        public const string RegisteredTo = "registered-to";
        /// <summary>
        /// Day when person was registered
        /// </summary>
        public const string RegisteredOn = "registered-on";
    }
}
