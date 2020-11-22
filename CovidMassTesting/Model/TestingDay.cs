using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Registration day
    /// </summary>
    public class TestingDay
    {
        /// <summary>
        /// Time
        /// </summary>
        public DateTimeOffset Time { get; set; }
        /// <summary>
        /// Registrations stats
        /// </summary>
        public int Registrations { get; set; }
        /// <summary>
        /// Healthy
        /// </summary>
        public int Healthy { get; set; }
        /// <summary>
        /// Sick
        /// </summary>
        public int Sick { get; set; }
    }
}
