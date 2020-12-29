using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    /// <summary>
    /// Place repository interface for dependency injection
    /// </summary>
    public interface IPlaceProviderRepository
    {

        /// <summary>
        /// Admin can remove place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public Task Delete(PlaceProvider place);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
        /// <summary>
        /// Registers place provider
        /// </summary>
        /// <param name="testingPlaceProvider"></param>
        /// <returns></returns>
        public Task<PlaceProvider> Register(PlaceProvider testingPlaceProvider);
        /// <summary>
        /// Returns list of public information of test providers
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<PlaceProviderPublic>> ListPublic();
        /// <summary>
        /// Returns list of test providers with full information
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<PlaceProvider>> ListPrivate(string email);
        /// <summary>
        /// Get price
        /// </summary>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <param name="from"></param>
        /// <param name="until"></param>
        /// <returns></returns>
        public decimal GetPriceWithoutVAT(string slaLevel, int registrations, string currency, DateTimeOffset from, DateTimeOffset until);
        /// <summary>
        /// PriceWithoutVAT SLA
        /// </summary>
        /// <param name="slaLevel"></param>
        /// <param name="currency"></param>
        /// <param name="from"></param>
        /// <param name="until"></param>
        /// <returns></returns>
        public decimal GetPriceWithoutVATSLA(string slaLevel, string currency, DateTimeOffset from, DateTimeOffset until);
        /// <summary>
        /// PriceWithoutVAT Registrations
        /// </summary>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public decimal GetPriceWithoutVATRegistrations(int registrations, string currency);
        /// <summary>
        /// issues proforma invoice
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <param name="slaFrom"></param>
        /// <param name="slaUntil"></param>
        /// <returns></returns>
        public Task<ProformaInvoice> IssueProformaInvoice(string placeProviderId, string slaLevel, int registrations, string currency, DateTimeOffset slaFrom, DateTimeOffset slaUntil);
        /// <summary>
        /// issues proforma invoice only for registrations
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public Task<ProformaInvoice> IssueProformaInvoiceRegistrations(string placeProviderId, int registrations, string currency);
        /// <summary>
        /// GetVATMultiplier
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        public decimal GetVATMultiplier(string country);
        /// <summary>
        /// Check permissions
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeProviderId"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        public Task<bool> InAnyGroup(string email, string placeProviderId, string[] vs);
        /// <summary>
        /// Returns place provider
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<PlaceProvider> GetPlaceProvider(string placeProviderId);
        /// <summary>
        /// Return user groups scoped to place provider
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<HashSet<string>> GetUserGroups(string email, string placeProviderId);
        /// <summary>
        /// Save place provider
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public Task<PlaceProvider> SetPlaceProvider(PlaceProvider place);
        /// <summary>
        /// Allocates person to place provider
        /// </summary>
        /// <param name="allocation"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<PersonAllocation> AllocatePerson(PersonAllocation allocation, string placeId);
        /// <summary>
        /// List allocations
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<IEnumerable<PersonAllocation>> ListAllocations(string placeId);
        /// <summary>
        /// Removes allocation
        /// </summary>
        /// <param name="allocationId"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<bool> RemovePersonAllocation(string allocationId, string placeId);
        /// <summary>
        /// Administrator is allowed to list pp products
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        Task<IEnumerable<Product>> ListProducts(string placeProviderId);
        /// <summary>
        /// Administrator is allowed to create product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        Task<Product> AddProduct(string placeProviderId, Product product);
        /// <summary>
        /// Administrator is allowed to update product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        Task<Product> SetProduct(string placeProviderId, Product product);
        /// <summary>
        /// Administrator is allowed to delete product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="product"></param>
        Task<bool> DeleteProduct(string placeProviderId, Product product);
    }
}
