using CovidMassTesting.Model;
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
    public class PlaceRepository : Repository.RedisRepository.PlaceRepository
    {
        private readonly ConcurrentDictionary<string, Place> data = new ConcurrentDictionary<string, Place>();
        public PlaceRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.PlaceRepository>(), redisCacheClient)
        {

        }
        public override async Task<Place> Set(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            data[place.Id] = place;
            return place;
        }
        public override async Task<Place> GetPlace(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                throw new ArgumentException($"'{nameof(placeId)}' cannot be null or empty", nameof(placeId));
            }

            return data[placeId];
        }
        public override async Task<IEnumerable<Place>> ListAll()
        {
            return data.Values;
        }
        public virtual async Task Delete(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }
            data.TryRemove(place.Id, out var _);
        }
        public override async Task<int> DropAllData()
        {
            var ret = data.Count;
            data.Clear();
            return ret;
        }
    }
}
