using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    /// <summary>
    /// Redis place repository
    /// </summary>
    public class PlaceProviderRepository : IPlaceProviderRepository
    {
        private readonly ILogger<PlaceProviderRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_PLACES_OBJECTS = "PP";
        private readonly string REDIS_KEY_PRO_INVOICES_OBJECTS = "PP_PRO_INVOICE";
        private readonly string REDIS_KEY_REAL_INVOICES_OBJECTS = "PP_REAL_INVOICE";
        private readonly string REDIS_KEY_LAST_PRO_INVOICE = "PP_LAST_PRO_INVOICE";
        private readonly string REDIS_KEY_LAST_REAL_INVOICE = "PP_LAST_REAL_INVOICE";
        private int ProInvoiceFormat = 2010100000;
        private int RealInvoiceFormat = 2010200000;
        private decimal TaxRate = 1.20M;
        private string TaxDomicil = "SK";
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        public PlaceProviderRepository(
            IConfiguration configuration,
            ILogger<PlaceProviderRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
        }

        #region PRO INVOICE

        /// <summary>
        /// Set place
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        public virtual async Task<ProformaInvoice> SetProInvoice(ProformaInvoice invoice)
        {
            if (invoice is null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            try
            {
                var last = ProInvoiceFormat;
                try
                {
                    var lastCandidate = await ProInvoiceGetLastId();
                    if (lastCandidate.HasValue) last = lastCandidate.Value;
                }
                catch { }
                if (invoice.InvoiceId != last + 1) throw new Exception("Invalid invoice number");

                await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_PRO_INVOICES_OBJECTS}", invoice.InvoiceId.ToString(), invoice);
                return invoice;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                throw;
            }
        }

        /// <summary>
        /// Get last id
        /// </summary>
        /// <returns></returns>
        public virtual Task<int?> ProInvoiceGetLastId()
        {
            return redisCacheClient.Db0.GetAsync<int?>($"{configuration["db-prefix"]}{REDIS_KEY_LAST_PRO_INVOICE}");
        }
        /// <summary>
        /// Get last id
        /// </summary>
        /// <returns></returns>
        public virtual Task<bool> ProInvoiceSetLastId(int id)
        {
            return redisCacheClient.Db0.AddAsync($"{configuration["db-prefix"]}{REDIS_KEY_LAST_PRO_INVOICE}", id);
        }
        /// <summary>
        /// List all
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<ProformaInvoice>> ListAllProInvoices()
        {
            return redisCacheClient.Db0.HashValuesAsync<ProformaInvoice>($"{configuration["db-prefix"]}{REDIS_KEY_PRO_INVOICES_OBJECTS}");
        }
        #endregion

        #region PRO INVOICE

        /// <summary>
        /// Set real invoice
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        public virtual async Task<Invoice> SetRealInvoice(Invoice invoice)
        {
            if (invoice is null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            try
            {
                var last = ProInvoiceFormat;
                try
                {
                    var lastCandidate = await RealInvoiceGetLastId();
                    if (lastCandidate.HasValue) last = lastCandidate.Value;
                }
                catch { }
                if (invoice.InvoiceId != last + 1) throw new Exception("Invalid invoice number");

                await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_REAL_INVOICES_OBJECTS}", invoice.InvoiceId.ToString(), invoice);
                return invoice;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                throw;
            }
        }

        /// <summary>
        /// Get last id
        /// </summary>
        /// <returns></returns>
        public virtual Task<int?> RealInvoiceGetLastId()
        {
            return redisCacheClient.Db0.GetAsync<int?>($"{configuration["db-prefix"]}{REDIS_KEY_LAST_REAL_INVOICE}");
        }
        /// <summary>
        /// Get last id
        /// </summary>
        /// <returns></returns>
        public virtual Task<bool> RealInvoiceSetLastId(int id)
        {
            return redisCacheClient.Db0.AddAsync($"{configuration["db-prefix"]}{REDIS_KEY_LAST_REAL_INVOICE}", id);
        }
        /// <summary>
        /// List all
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<Invoice>> ListAllRealInvoices()
        {
            return redisCacheClient.Db0.HashValuesAsync<Invoice>($"{configuration["db-prefix"]}{REDIS_KEY_REAL_INVOICES_OBJECTS}");
        }
        #endregion

        /// <summary>
        /// Get place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public virtual Task<PlaceProvider> GetPlaceProvider(string placeId)
        {
            return redisCacheClient.Db0.HashGetAsync<PlaceProvider>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", placeId);
        }
        /// <summary>
        /// Set place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public virtual async Task<PlaceProvider> SetPlaceProvider(PlaceProvider place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            try
            {
                await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.PlaceProviderId.ToString(), place);
                return place;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                throw;
            }
        }



        /// <summary>
        /// Deletes place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public virtual async Task Delete(PlaceProvider place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.PlaceProviderId);
        }
        /// <summary>
        /// List all
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<PlaceProvider>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<PlaceProvider>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}");
        }
        /// <summary>
        /// Drop all data in repository
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> DropAllData()
        {
            var ret = 0;
            foreach (var place in await ListAll())
            {
                ret++;
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.PlaceProviderId);
            }
            return ret;
        }
        /// <summary>
        /// Calculate price of service
        /// </summary>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <param name="from"></param>
        /// <param name="until"></param>
        /// <returns></returns>
        public decimal GetPriceWithoutVAT(string slaLevel, int registrations, string currency, DateTimeOffset from, DateTimeOffset until)
        {
            return GetPriceWithoutVATSLA(slaLevel, currency, from, until) +
                GetPriceWithoutVATRegistrations(registrations, currency);
        }
        /// <summary>
        /// Calculate price of service
        /// </summary>
        /// <param name="slaLevel"></param>
        /// <param name="currency"></param>
        /// <param name="from"></param>
        /// <param name="until"></param>
        /// <returns></returns>
        public decimal GetPriceWithoutVATSLA(string slaLevel, string currency, DateTimeOffset from, DateTimeOffset until)
        {
            var days = Math.Abs((until.Date - from.Date).Days);
            var price = 0M;
            switch (slaLevel)
            {
                case SLALevel.Bronze:
                    price += days * 20;
                    break;
                case SLALevel.Silver:
                    price += days * 100;
                    break;
                case SLALevel.Gold:
                    price += days * 10000;
                    break;
            }

            switch (currency)
            {
                case Currency.USD:
                    price = price * 1.3M;
                    break;
                case Currency.CZK:
                    price = price * 27M;
                    break;
            }

            return Math.Round(price, 2);
        }
        /// <summary>
        /// Calculate price of service
        /// </summary>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public decimal GetPriceWithoutVATRegistrations(int registrations, string currency)
        {
            var price = 0M;
            price += registrations * 0.2M;
            switch (currency)
            {
                case Currency.USD:
                    price = price * 1.3M;
                    break;
                case Currency.CZK:
                    price = price * 27M;
                    break;
            }
            return Math.Round(price, 2);
        }
        /// <summary>
        /// Admin can list private info
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PlaceProvider>> ListPrivate(string email)
        {
            return (await ListAll()).Where(p => p.MainEmail == email || (p.Group2Emails.ContainsKey(Groups.Admin) && p.Group2Emails[Groups.Admin].Contains(email)));
        }
        /// <summary>
        /// Public info of all place providers
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<PlaceProviderPublic>> ListPublic()
        {
            return (await ListAll()).Select(p => p.ToPublic());
        }
        /// <summary>
        /// Registers as place provider
        /// </summary>
        /// <param name="placeProvider"></param>
        /// <returns></returns>
        public async Task<PlaceProvider> Register(PlaceProvider placeProvider)
        {
            if (string.IsNullOrEmpty(placeProvider?.CompanyId)) throw new Exception("Company trade registry ID has not been entered");
            placeProvider.PlaceProviderId = placeProvider.CompanyId.Trim();
            var place = await GetPlaceProvider(placeProvider.PlaceProviderId);
            if (place != null) { throw new Exception("Place provider with the specified company id already exists. Please contact administrator"); }
            return await SetPlaceProvider(placeProvider);
        }
        /// <summary>
        /// Issues the proforma invoice for registrations
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public async Task<ProformaInvoice> IssueProformaInvoiceRegistrations(string placeProviderId, int registrations, string currency)
        {
            var placeProvider = await GetPlaceProvider(placeProviderId);
            if (placeProvider == null) throw new Exception("Place provider not found");
            var last = ProInvoiceFormat;
            try
            {
                var lastCandidate = await ProInvoiceGetLastId();
                if (lastCandidate.HasValue) last = lastCandidate.Value;
            }
            catch { }

            var p1 = GetPriceWithoutVATRegistrations(registrations, currency);

            var multiplier = GetVATMultiplier(placeProvider.Country);
            var p1withVAT = decimal.Round(GetPriceWithoutVATRegistrations(registrations, currency) * multiplier, 2);

            var invoice = new ProformaInvoice()
            {
                InvoiceId = last + 1,
                BuyerID = placeProvider.CompanyId,
                BuyerName = placeProvider.CompanyName,
                BuyerVAT = placeProvider.VAT,
                Currency = currency,
                Description = $"Registrations {registrations}",
                IssuedOn = DateTimeOffset.Now,
                Payable = DateTimeOffset.Now.AddDays(14),
                PlaceProviderId = placeProviderId,
                Registrations = registrations,
                PriceNoVATRegistrations = p1,
                PriceNoVATSLA = 0M,
                PriceNoVATTotal = p1,
                PriceWithVATRegistrations = p1withVAT,
                PriceWithVATSLA = 0M,
                PriceWithVATTotal = p1withVAT
            };
            return await SetProInvoice(invoice);
            ///@todo .. send invoice at this point to all admins and all accountants by email
        }
        /// <summary>
        /// Issues the proforma invoice
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <param name="slaFrom"></param>
        /// <param name="slaUntil"></param>
        /// <returns></returns>
        public async Task<ProformaInvoice> IssueProformaInvoice(string placeProviderId, string slaLevel, int registrations, string currency, DateTimeOffset slaFrom, DateTimeOffset slaUntil)
        {
            var placeProvider = await GetPlaceProvider(placeProviderId);
            if (placeProvider == null) throw new Exception("Place provider not found");
            var last = ProInvoiceFormat;
            try
            {
                var lastCandidate = await ProInvoiceGetLastId();
                if (lastCandidate.HasValue) last = lastCandidate.Value;
            }
            catch { }

            var p1 = GetPriceWithoutVATRegistrations(registrations, currency);
            var p2 = GetPriceWithoutVATSLA(slaLevel, currency, slaFrom, slaUntil);
            var pTotal = GetPriceWithoutVAT(slaLevel, registrations, currency, slaFrom, slaUntil);

            var multiplier = GetVATMultiplier(placeProvider.Country);
            var p1withVAT = decimal.Round(GetPriceWithoutVATRegistrations(registrations, currency) * multiplier, 2);
            var p2withVAT = decimal.Round(GetPriceWithoutVATSLA(slaLevel, currency, slaFrom, slaUntil) * multiplier, 2);
            var pTotalwithVAT = p1withVAT + p2withVAT;

            var invoice = new ProformaInvoice()
            {
                InvoiceId = last + 1,
                BuyerID = placeProvider.CompanyId,
                BuyerName = placeProvider.CompanyName,
                BuyerVAT = placeProvider.VAT,
                Currency = currency,
                Description = $"SLA: {slaLevel} {slaFrom.ToString("dd.MM.yyyy")} {slaUntil.ToString("dd.MM.yyyy")} Registrations {registrations}",
                IssuedOn = DateTimeOffset.Now,
                Payable = DateTimeOffset.Now.AddDays(14),
                PlaceProviderId = placeProviderId,
                Registrations = registrations,
                PriceNoVATRegistrations = p1,
                PriceNoVATSLA = p2,
                PriceNoVATTotal = pTotal,
                PriceWithVATRegistrations = p1withVAT,
                PriceWithVATSLA = p2withVAT,
                PriceWithVATTotal = pTotalwithVAT
            };
            return await SetProInvoice(invoice);
            ///@todo .. send invoice at this point to all admins and all accountants by email
        }
        /// <summary>
        /// Issues the proforma invoice for registrations
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public async Task<ProformaInvoice> IssueRealInvoiceRegistrations(string placeProviderId, int registrations, string currency)
        {
            var placeProvider = await GetPlaceProvider(placeProviderId);
            if (placeProvider == null) throw new Exception("Place provider not found");
            var last = ProInvoiceFormat;
            try
            {
                var lastCandidate = await RealInvoiceGetLastId();
                if (lastCandidate.HasValue) last = lastCandidate.Value;
            }
            catch { }

            var p1 = GetPriceWithoutVATRegistrations(registrations, currency);

            var multiplier = GetVATMultiplier(placeProvider.Country);
            var p1withVAT = decimal.Round(GetPriceWithoutVATRegistrations(registrations, currency) * multiplier, 2);

            var invoice = new ProformaInvoice()
            {
                InvoiceId = last + 1,
                BuyerID = placeProvider.CompanyId,
                BuyerName = placeProvider.CompanyName,
                BuyerVAT = placeProvider.VAT,
                Currency = currency,
                Description = $"Registrations {registrations}",
                IssuedOn = DateTimeOffset.Now,
                Payable = DateTimeOffset.Now.AddDays(14),
                PlaceProviderId = placeProviderId,
                Registrations = registrations,
                PriceNoVATRegistrations = p1,
                PriceNoVATSLA = 0M,
                PriceNoVATTotal = p1,
                PriceWithVATRegistrations = p1withVAT,
                PriceWithVATSLA = 0M,
                PriceWithVATTotal = p1withVAT,

            };
            return await SetProInvoice(invoice);
            ///@todo .. send invoice at this point to all admins and all accountants by email
        }
        /// <summary>
        /// Issues the proforma invoice
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <param name="slaFrom"></param>
        /// <param name="slaUntil"></param>
        /// <returns></returns>
        public async Task<Invoice> IssueRealInvoice(string placeProviderId, string slaLevel, int registrations, string currency, DateTimeOffset slaFrom, DateTimeOffset slaUntil)
        {
            var placeProvider = await GetPlaceProvider(placeProviderId);
            if (placeProvider == null) throw new Exception("Place provider not found");
            var last = RealInvoiceFormat;
            try
            {
                var lastCandidate = await RealInvoiceGetLastId();
                if (lastCandidate.HasValue) last = lastCandidate.Value;
            }
            catch { }

            var p1 = GetPriceWithoutVATRegistrations(registrations, currency);
            var p2 = GetPriceWithoutVATSLA(slaLevel, currency, slaFrom, slaUntil);
            var pTotal = GetPriceWithoutVAT(slaLevel, registrations, currency, slaFrom, slaUntil);

            var multiplier = GetVATMultiplier(placeProvider.Country);
            var p1withVAT = decimal.Round(GetPriceWithoutVATRegistrations(registrations, currency) * multiplier, 2);
            var p2withVAT = decimal.Round(GetPriceWithoutVATSLA(slaLevel, currency, slaFrom, slaUntil) * multiplier, 2);
            var pTotalwithVAT = p1withVAT + p2withVAT;

            var invoice = new Invoice()
            {
                InvoiceId = last + 1,
                BuyerID = placeProvider.CompanyId,
                BuyerName = placeProvider.CompanyName,
                BuyerVAT = placeProvider.VAT,
                Currency = currency,
                Description = $"SLA: {slaLevel} {slaFrom.ToString("dd.MM.yyyy")} {slaUntil.ToString("dd.MM.yyyy")} Registrations {registrations}",
                IssuedOn = DateTimeOffset.Now,
                Payable = DateTimeOffset.Now.AddDays(14),
                PlaceProviderId = placeProviderId,
                Registrations = registrations,
                PriceNoVATRegistrations = p1,
                PriceNoVATSLA = p2,
                PriceNoVATTotal = pTotal,
                PriceWithVATRegistrations = p1withVAT,
                PriceWithVATSLA = p2withVAT,
                PriceWithVATTotal = pTotalwithVAT
            };
            return await SetRealInvoice(invoice);
            ///@todo .. send invoice at this point to all admins and all accountants by email
        }
        /// <summary>
        /// Return tax multiplier
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        public decimal GetVATMultiplier(string country)
        {
            if (country == TaxDomicil) return TaxRate;
            return 1;
        }

        public async Task<bool> InAnyGroup(string email, string placeProviderId, string[] role)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty", nameof(email));
            }

            if (string.IsNullOrEmpty(placeProviderId))
            {
                throw new ArgumentException($"'{nameof(placeProviderId)}' cannot be null or empty", nameof(placeProviderId));
            }

            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            if (role.Length == 0) return true;
            var place = await GetPlaceProvider(placeProviderId);
            foreach (var group in role)
            {
                if (place.Group2Emails.ContainsKey(group))
                {
                    if (place.Group2Emails[group].Contains(email)) return true;
                }
            }

            return false;
        }
    }
}
