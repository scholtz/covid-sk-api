using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Place provider sensitive data.. for example credentials to government ehealth service
    /// </summary>
    public class PlaceProviderSensitiveData
    {
        /// <summary>
        /// Id
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// eHealth username
        /// </summary>
        public string EZdravieUser { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        public string EZdraviePass { get; set; }
    }
}
