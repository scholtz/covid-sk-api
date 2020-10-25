using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
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
        private readonly string REDIS_KEY_PLACES_OBJECTS = "PLACE";
        private readonly string REDIS_KEY_PLACES_LIST = "PLACESLIST";

        public PlaceRepository(
            ILogger<PlaceRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
        }
        public virtual async Task<bool> Add(Place place)
        {
            try
            {
                if (!await redisCacheClient.Db0.HashSetAsync(REDIS_KEY_PLACES_OBJECTS, place.Id.ToString(), place, true))
                {
                    throw new Exception("Error creating place");
                }
                await redisCacheClient.Db0.SetAddAsync($"{REDIS_KEY_PLACES_LIST}_{place.Id}", $"{place.Id}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }

        public virtual async Task IncrementPlaceRegistrations(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Registrations++;
            await redisCacheClient.Db0.HashSetAsync(REDIS_KEY_PLACES_OBJECTS, placeId, update);
        }
        public virtual Task<Place> GetPlace(string placeId)
        {
            return redisCacheClient.Db0.HashGetAsync<Place>(REDIS_KEY_PLACES_OBJECTS, placeId);
        }
        public virtual Task<IEnumerable<Place>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<Place>(REDIS_KEY_PLACES_OBJECTS);
        }
    }
}
