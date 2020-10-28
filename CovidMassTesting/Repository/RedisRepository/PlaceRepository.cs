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
                if (!await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", place.Id.ToString(), place, true))
                {
                    throw new Exception("Error creating place");
                }
                return place;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                throw;
            }
        }

        public virtual async Task IncrementPlaceRegistrations(string placeId)
        {
            var update = await GetPlace(placeId);
            update.Registrations++;
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", placeId, update);
        }
        public virtual Task<Place> GetPlace(string placeId)
        {
            return redisCacheClient.Db0.HashGetAsync<Place>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}", placeId);
        }
        public virtual Task<IEnumerable<Place>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<Place>($"{configuration["db-prefix"]}{REDIS_KEY_PLACES_OBJECTS}");
        }
    }
}
