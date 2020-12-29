using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Product or service served by the place provider at testing places
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Category
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Name of the product or service
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Insurance only
        /// </summary>
        public bool InsuranceOnly { get; set; }
        /// <summary>
        /// Applied for all places
        /// </summary>
        public bool All { get; set; }
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Default price. Admin can set the pricing other then default at the specific places
        /// </summary>
        public decimal DefaultPrice { get; set; }
        /// <summary>
        /// Price currency
        /// </summary>
        public string DefaultPriceCurrency { get; set; }
        /// <summary>
        /// When the resource was created
        /// </summary>
        public DateTimeOffset CreatedOn { get; set; }
        /// <summary>
        /// Time when resource was last updated
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }

    }
}
