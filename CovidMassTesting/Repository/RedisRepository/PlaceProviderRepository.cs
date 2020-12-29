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
        private readonly IPlaceRepository placeRepository;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_PLACES_OBJECTS = "PP";
        private readonly string REDIS_KEY_PRO_INVOICES_OBJECTS = "PP_PRO_INVOICE";
        private readonly string REDIS_KEY_REAL_INVOICES_OBJECTS = "PP_REAL_INVOICE";
        private readonly string REDIS_KEY_LAST_PRO_INVOICE = "PP_LAST_PRO_INVOICE";
        private readonly string REDIS_KEY_LAST_REAL_INVOICE = "PP_LAST_REAL_INVOICE";

        private readonly int ProInvoiceFormat = 2010100000;
        private readonly int RealInvoiceFormat = 2010200000;
        private readonly decimal TaxRate = 1.20M;
        private readonly string TaxDomicil = "SK";
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="placeRepository"></param>
        public PlaceProviderRepository(
            IConfiguration configuration,
            ILogger<PlaceProviderRepository> logger,
            IRedisCacheClient redisCacheClient,
            IPlaceRepository placeRepository
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
            this.placeRepository = placeRepository;
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
                    price *= 1.3M;
                    break;
                case Currency.CZK:
                    price *= 27M;
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
                    price *= 1.3M;
                    break;
                case Currency.CZK:
                    price *= 27M;
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
            return (await ListAll()).Where(
                p => p.MainEmail == email
                    || (p.Group2Emails.ContainsKey(Groups.Admin) && p.Group2Emails[Groups.Admin].Contains(email))
                    || ((p.Users?.Any(u => u.Email == email) == true)
            ));
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
            PlaceProvider place = null;
            try
            {
                place = await GetPlaceProvider(placeProvider.PlaceProviderId);
            }
            catch (Exception exc) { logger.LogError(exc, exc.Message); }
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
                Description = $"SLA: {slaLevel} {slaFrom:dd.MM.yyyy} {slaUntil:dd.MM.yyyy} Registrations {registrations}",
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
                Description = $"SLA: {slaLevel} {slaFrom:dd.MM.yyyy} {slaUntil:dd.MM.yyyy} Registrations {registrations}",
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

        /// <summary>
        /// Check if user is in specified group in place provider company
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeProviderId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
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
            if (role.Contains(Groups.PPAdmin))
            {
                if (place.MainEmail == email) return true;
            }

            if (place.Allocations != null)
            {
                foreach (var allocation in place.Allocations)
                {
                    if (allocation.User != email) continue;
                    foreach (var group in role)
                    {
                        if (allocation.Role == group)
                        {
                            return true;
                        }
                    }
                }
            }

            foreach (var group in role)
            {
                if (place.Group2Emails.ContainsKey(group))
                {
                    if (place.Group2Emails[group].Contains(email)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return user groups scoped to place provider
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public async Task<HashSet<string>> GetUserGroups(string email, string placeProviderId)
        {
            var pp = await GetPlaceProvider(placeProviderId);
            var ret = new HashSet<string>();
            if (pp == null) return ret;
            if (pp.MainEmail == email) ret.Add(Groups.PPAdmin);
            foreach (var group in pp.Allocations?.Select(a => a.Role).Distinct())
            {
                if (!ret.Contains(group)) ret.Add(group);
            }
            return ret;
        }
        /// <summary>
        /// Allocate person to place
        /// </summary>
        /// <param name="allocation"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public async Task<PersonAllocation> AllocatePerson(PersonAllocation allocation, string placeId)
        {
            if (allocation.Role != Groups.DataExporter &&
                allocation.Role != Groups.DocumentManager &&
                allocation.Role != Groups.MedicLab &&
                allocation.Role != Groups.MedicTester &&
                allocation.Role != Groups.RegistrationManager &&
                allocation.Role != Groups.Helper
                )
            {
                throw new Exception("Wrong role defined in the allocation");
            }
            var place = await placeRepository.GetPlace(placeId);
            if (place == null) throw new Exception("Place not found");
            if (string.IsNullOrEmpty(place.PlaceProviderId)) throw new Exception("Unable to find place within scope of place provider");
            var pp = await GetPlaceProvider(place.PlaceProviderId);
            if (pp == null) throw new Exception("Unable to find place provider");
            if (pp.Allocations == null) pp.Allocations = new List<PersonAllocation>();
            allocation.Id = Guid.NewGuid().ToString();
            allocation.PlaceId = placeId;
            pp.Allocations.Add(allocation);
            await SetPlaceProvider(pp);
            return allocation;
        }

        /// <summary>
        /// Removes person allocation at place
        /// </summary>
        /// <param name="allocationId"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public async Task<bool> RemovePersonAllocation(string allocationId, string placeId)
        {
            var place = await placeRepository.GetPlace(placeId);
            if (place == null) throw new Exception("Place not found");
            if (string.IsNullOrEmpty(place.PlaceProviderId)) throw new Exception("Unable to find place within scope of place provider");
            var pp = await GetPlaceProvider(place.PlaceProviderId);
            if (pp == null) throw new Exception("Unable to find place provider");
            if (pp.Allocations == null) pp.Allocations = new List<PersonAllocation>();
            var allocationToRemove = pp.Allocations.FirstOrDefault(a => a.Id == allocationId);
            if (allocationToRemove == null) throw new Exception("Allocation not found");
            pp.Allocations.Remove(allocationToRemove);
            await SetPlaceProvider(pp);
            return true;
        }

        /// <summary>
        /// List allocations
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PersonAllocation>> ListAllocations(string placeId)
        {
            var place = await placeRepository.GetPlace(placeId);
            if (place == null) throw new Exception("Place not found");
            if (string.IsNullOrEmpty(place.PlaceProviderId)) throw new Exception("Unable to find place within scope of place provider");
            var pp = await GetPlaceProvider(place.PlaceProviderId);
            return pp.Allocations?.Where(p => p.PlaceId == placeId);
        }
        /// <summary>
        /// Administrator is allowed to list pp products
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Product>> ListProducts(string placeProviderId)
        {
            var pp = await GetPlaceProvider(placeProviderId);
            if (pp == null) throw new Exception("Unable to find place provider");

            return pp.Products;
        }

        /// <summary>
        /// Administrator is allowed to create product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public async Task<Product> AddProduct(string placeProviderId, Product product)
        {
            var pp = await GetPlaceProvider(placeProviderId);
            if (pp == null) throw new Exception("Unable to find place provider");

            if (pp.Products == null) pp.Products = new List<Product>();
            if (pp.Products.Any(p => p.Id == product.Id)) throw new Exception("Product with same ID already exists");

            pp.Products.Add(product);
            await SetPlaceProvider(pp);
            return product;
        }
        /// <summary>
        /// Administrator is allowed to update product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="product"></param>
        /// <returns></returns>

        public async Task<Product> SetProduct(string placeProviderId, Product product)
        {
            var pp = await GetPlaceProvider(placeProviderId);
            if (pp == null) throw new Exception("Unable to find place provider");

            if (pp.Products == null) pp.Products = new List<Product>();
            var old = pp.Products.FirstOrDefault(p => p.Id == product.Id);
            if (old != null)
            {
                product.CreatedOn = old.CreatedOn;
                pp.Products.Remove(old);
            }
            product.LastUpdate = DateTimeOffset.Now;
            pp.Products.Add(product);
            await SetPlaceProvider(pp);
            return product;
        }

        /// <summary>
        /// Administrator is allowed to delete product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="product"></param>
        public async Task<bool> DeleteProduct(string placeProviderId, Product product)
        {
            var pp = await GetPlaceProvider(placeProviderId);
            if (pp == null) throw new Exception("Unable to find place provider");

            if (pp.Products == null) pp.Products = new List<Product>();
            var old = pp.Products.FirstOrDefault(p => p.Id == product.Id);
            if (old == null) throw new Exception("Product does not exists");
            pp.Products.Remove(old);
            await SetPlaceProvider(pp);
            return true;
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
            foreach (var invoice in await ListAllProInvoices())
            {
                ret++;
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PRO_INVOICES_OBJECTS}", invoice.InvoiceId.ToString());
            }
            foreach (var invoice in await ListAllRealInvoices())
            {
                ret++;
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_REAL_INVOICES_OBJECTS}", invoice.InvoiceId.ToString());
            }

            await redisCacheClient.Db0.RemoveAsync(REDIS_KEY_LAST_PRO_INVOICE);
            await redisCacheClient.Db0.RemoveAsync(REDIS_KEY_LAST_REAL_INVOICE);
            return ret;
        }

    }
}
