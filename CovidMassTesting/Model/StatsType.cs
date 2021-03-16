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
