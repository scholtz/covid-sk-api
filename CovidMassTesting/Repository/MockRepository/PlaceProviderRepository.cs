using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Helpers;

namespace CovidMassTesting.Repository.MockRepository
{
    /// <summary>
    /// Place mock repository
    /// </summary>
    public class PlaceProviderRepository : Repository.RedisRepository.PlaceProviderRepository
    {
        private readonly IConfiguration configuration;
        private readonly ConcurrentDictionary<string, PlaceProvider> data = new ConcurrentDictionary<string, PlaceProvider>();
        private readonly ConcurrentDictionary<string, string> dataEncoded = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="placeRepository"></param>
        public PlaceProviderRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IPlaceRepository placeRepository
            ) : base(
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.PlaceProviderRepository>(),
                redisCacheClient,
                placeRepository)
        {
            this.configuration = configuration;
        }
        /// <summary>
        /// set
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public override async Task<PlaceProvider> SetPlaceProvider(PlaceProvider place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            data[place.PlaceProviderId] = place;
            return place;
        }
        /// <summary>
        /// get
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public override async Task<PlaceProvider> GetPlaceProvider(string placeProviderId)
        {
            if (string.IsNullOrEmpty(placeProviderId))
            {
                throw new ArgumentException($"'{nameof(placeProviderId)}' cannot be null or empty", nameof(placeProviderId));
            }

            return data[placeProviderId];
        }
        /// <summary>
        /// List all
        /// </summary>
        /// <returns></returns>
        public override async Task<IEnumerable<PlaceProvider>> ListAll()
        {
            return data.Values;
        }
        /// <summary>
        /// Delete place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public virtual async Task DeletePlace(PlaceProvider place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }
            data.TryRemove(place.PlaceProviderId, out var _);
        }
        /// <summary>
        /// CombinePPWithCategory
        /// </summary>
        /// <param name="category"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public override async Task<bool> CombinePPWithCategory(string category, string placeProviderId)
        {
            return true;
        }
        /// <summary>
        /// ListPPIdsByCategory
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<string>> ListPPIdsByCategory(string category)
        {
            return data.Values.Where(pp => pp.Products?.Any(prod => prod.Category == category) == true).Select(pp => pp.PlaceProviderId);
        }

        /// <summary>
        /// Deletes all data
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = data.Count;
            data.Clear();
            return ret;
        }

        public async override Task<bool> SetPlaceProviderSensitiveData(PlaceProviderSensitiveData data, bool mustBeNew)
        {
            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (mustBeNew && dataEncoded.ContainsKey(data.PlaceProviderId))
            {
                throw new Exception("Error setting sensitive data");
            }
            dataEncoded[data.PlaceProviderId] = encoded;
            return true;
        }
        public virtual async Task<PlaceProviderSensitiveData> GetPlaceProviderSensitiveData(string placeProviderId)
        {
            var encoded = dataEncoded[placeProviderId];
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<PlaceProviderSensitiveData>(decoded);
        }
    }
}
