using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Allocate person to the testing place with specified role
    /// </summary>
    public class PersonAllocation
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Role
        /// </summary>
        public string Role { get; set; }
        /// <summary>
        /// Start
        /// </summary>
        public DateTimeOffset Start { get; set; }
        /// <summary>
        /// End
        /// </summary>
        public DateTimeOffset End { get; set; }
        /// <summary>
        /// Allocated place id
        /// </summary>
        public string PlaceId { get; set; }
    }
}
