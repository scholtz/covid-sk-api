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
    }
}
