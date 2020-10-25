using CovidMassTesting.Model;
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
        private ConcurrentDictionary<string, Place> data = new ConcurrentDictionary<string, Place>();
        public PlaceRepository(
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(loggerFactory.CreateLogger<Repository.RedisRepository.PlaceRepository>(), redisCacheClient)
        {
            Add(new Place()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Škola AA",

                Address = "Bratislavská 1, Pezinok",
                Lat = 48.28524902921143M,
                Lng = 17.256517410278324M,
                IsDriveIn = true,
                IsWalkIn = false,
                Registrations = 0
            }).Wait();

            Add(new Place()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Odberné miesto 2",
                Address = "Pražská 11, Pezinok",
                Lat = 48.29467191641477M,
                Lng = 17.26587295532227M,
                IsDriveIn = false,
                IsWalkIn = true,
                Registrations = 0
            }).Wait();
            Add(new Place()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Odberné miesto 3",
                Address = "Pražská 10, Pezinok",
                Lat = 48.289218275462225M,
                Lng = 17.272996902465824M,
                IsDriveIn = true,
                IsWalkIn = true,
                Registrations = 0
            }).Wait();
        }
        public override async Task<bool> Add(Place place)
        {
            data[place.Id] = place;
            return true;
        }
        public override async Task<Place> GetPlace(string placeId)
        {
            return data[placeId];
        }
        public override async Task IncrementPlaceRegistrations(string placeId)
        {
            data[placeId].Registrations++;
        }
        public override async Task<IEnumerable<Place>> ListAll()
        {
            return data.Values;
        }
    }
}
