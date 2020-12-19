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
    public class PlaceRepository : IPlaceRepository
    {
        private readonly ILogger<PlaceRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_PLACES_OBJECTS = "PLACE";
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>

        public PlaceRepository(
            IConfiguration configuration,
            ILogger<PlaceRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
        }
        /// <summary>
        /// Set place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public virtual async Task<Place> SetPlace(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            try
            {
                await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.Id.ToString(), place);

                return await GetPlace(place.Id);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                throw;
            }
        }
        /// <summary>
        /// Decrement registrations
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public async Task DecrementPlaceRegistrations(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Registrations--;
            await SetPlace(update);
        }
        /// <summary>
        /// Increment registrations
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public async Task IncrementPlaceRegistrations(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Registrations++;
            await SetPlace(update);
        }
        /// <summary>
        /// increment health stats
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public virtual async Task IncrementPlaceHealthy(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Healthy++;
            await SetPlace(update);
        }
        /// <summary>
        /// Increment sick stats
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public virtual async Task IncrementPlaceSick(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Sick++;
            await SetPlace(update);
        }
        /// <summary>
        /// Get place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public virtual Task<Place> GetPlace(string placeId)
        {
            return redisCacheClient.Db0.HashGetAsync<Place>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", placeId);
        }
        /// <summary>
        /// List all
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<Place>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<Place>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}");
        }
        /// <summary>
        /// Deletes place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public virtual async Task Delete(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.Id);
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
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.Id);
            }
            return ret;
        }

    }
}
