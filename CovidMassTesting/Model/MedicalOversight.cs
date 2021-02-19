using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Oversight
    /// </summary>
    public class MedicalOversight
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Name of medical 
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// From 
        /// </summary>
        public DateTimeOffset? From { get; set; }
        /// <summary>
        /// Until
        /// </summary>
        public DateTimeOffset? Until { get; set; }
    }
}
