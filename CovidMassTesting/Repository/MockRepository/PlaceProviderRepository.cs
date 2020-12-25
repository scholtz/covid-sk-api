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

namespace CovidMassTesting.Repository.MockRepository
{
    /// <summary>
    /// Place mock repository
    /// </summary>
    public class PlaceProviderRepository : Repository.RedisRepository.PlaceProviderRepository
    {
        private readonly ConcurrentDictionary<string, PlaceProvider> data = new ConcurrentDictionary<string, PlaceProvider>();
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
        public override async Task<PlaceProvider> GetPlaceProvider(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                throw new ArgumentException($"'{nameof(placeId)}' cannot be null or empty", nameof(placeId));
            }

            return data[placeId];
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
        /// Deletes all data
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = data.Count;
            data.Clear();
            return ret;
        }
    }
}
