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
    public class PlaceRepository : IPlaceRepository
    {
        private readonly ILogger<PlaceRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_PLACES_OBJECTS = "PLACE";

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
        public virtual async Task<Place> Set(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            try
            {
                await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.Id.ToString(), place);

                return place;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                throw;
            }
        }

        public async Task DecrementPlaceRegistrations(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Registrations--;
            await Set(update);
        }
        public async Task IncrementPlaceRegistrations(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Registrations++;
            await Set(update);
        }
        public virtual async Task IncrementPlaceHealthy(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Healthy++;
            await Set(update);
        }
        public virtual async Task IncrementPlaceSick(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Sick++;
            await Set(update);
        }
        public virtual Task<Place> GetPlace(string placeId)
        {
            return redisCacheClient.Db0.HashGetAsync<Place>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", placeId);
        }
        public virtual Task<IEnumerable<Place>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<Place>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}");
        }

        public virtual async Task Delete(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.Id);
        }

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
