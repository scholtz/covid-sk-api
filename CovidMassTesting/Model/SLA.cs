using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Hospitals can negotiate multiple level of service level agreement
    /// 
    /// This determines how much hardware is allocated to the place provider and his minimum level of requests per seconds.
    /// 
    /// Shared means no hosting costs with no guarantee
    /// Bronze means low costs eg 20 EUR per day with dedicated hw. Usually purchased by small testing locations.
    /// Silver means mid costs eg 100 EUR per day with dedicated HW. Usually purchased by big testing locations or cities with multiple places.
    /// Gold means max availablilty dedicated for full scale government testing. For example 5 mil. people per two days.
    /// 
    /// </summary>
    public class SLA
    {
        /// <summary>
        /// ID
        /// </summary>
        public string SLAId { get; set; }
        /// <summary>
        /// Provider
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Purchased SLA Type 
        /// 
        /// Shared | Bronze | Silver | Gold
        /// </summary>
        public string SLAType { get; set; }
        /// <summary>
        /// Valid From
        /// </summary>
        public DateTimeOffset From { get; set; }
        /// <summary>
        /// Valid Until
        /// </summary>
        public DateTimeOffset Until { get; set; }
    }
    /// <summary>
    /// SLA constants
    /// </summary>
    public static class SLALevel
    {
        /// <summary>
        /// shared infrastructure
        /// </summary>
        public const string Shared = "shared";
        /// <summary>
        /// bronze infrastructure
        /// </summary>
        public const string Bronze = "bronze";
        /// <summary>
        /// silver infrastructure
        /// </summary>
        public const string Silver = "silver";
        /// <summary>
        /// gold infrastructure
        /// </summary>
        public const string Gold = "gold";
        /// <summary>
        /// validates sla level
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool ValidateSLA(this string input)
        {
            switch (input)
            {
                case Shared:
                case Bronze:
                case Silver:
                case Gold:
                    return true;
            }
            return false;
        }
    }
}
