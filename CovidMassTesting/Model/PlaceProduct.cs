using CovidMassTesting.Repository.Interface;
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
        /// <summary>
        /// Only for parents who need the test for the school verification
        /// </summary>
        public bool? SchoolOnly { get; set; } = false;
        /// <summary>
        /// Corporations may check this on and only its employees are eligible for the testing
        /// </summary>
        public bool? EmployeesOnly { get; set; } = false;
        /// <summary>
        /// Default for eHealth connection
        /// </summary>
        public bool? EHealthDefault { get; set; } = false;
        /// <summary>
        /// Corporations may check this on and only its employees can self register
        /// </summary>
        public bool? EmployeesRegistration { get; set; } = false;
        /// <summary>
        /// Collect insurance organisation
        /// </summary>
        public bool? CollectInsurance { get; set; } = true;
        /// <summary>
        /// Extend
        /// </summary>
        /// <param name="placeProviderRepository"></param>
        /// <returns></returns>
        internal async Task<PlaceProductWithPlace> ToExtendedModel(IPlaceProviderRepository placeProviderRepository)
        {
            var pp = await placeProviderRepository.GetPlaceProvider(PlaceProviderId);
            return new PlaceProductWithPlace()
            {
                Id = this.Id,
                CustomPrice = this.CustomPrice,
                From = From,
                InsuranceOnly = InsuranceOnly,
                CollectInsurance = CollectInsurance,
                EmployeesOnly = EmployeesOnly,
                EmployeesRegistration = EmployeesRegistration,
                SchoolOnly = SchoolOnly,
                EHealthDefault = EHealthDefault,
                PlaceId = PlaceId,
                PlaceProviderId = PlaceProviderId,
                Price = Price,
                PriceCurrency = PriceCurrency,
                Product = pp?.Products?.FirstOrDefault(pr => pr.Id == ProductId),
                ProductId = ProductId,
                Until = Until,
            };
        }
    }
}
