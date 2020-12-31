using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Product specified for specific place
    /// </summary>
    public class PlaceProduct
    {
        /// <summary>
        /// Record Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Place id
        /// </summary>
        public string PlaceId { get; set; }
        /// <summary>
        /// Product id
        /// </summary>
        public string ProductId { get; set; }
        /// <summary>
        /// Time from when the product is provided
        /// </summary>
        public DateTimeOffset? From { get; set; }
        /// <summary>
        /// Time until when the product is provided
        /// </summary>
        public DateTimeOffset? Until { get; set; }
        /// <summary>
        /// Has custom price. If true, the price from this object is applied. If false, default price from product is applied
        /// </summary>
        public bool CustomPrice { get; set; }
        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// Currency
        /// </summary>
        public string PriceCurrency { get; set; }
        /// <summary>
        /// Place provider
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Insurance only
        /// </summary>
        public bool InsuranceOnly { get; set; }
    }
}
