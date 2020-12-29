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
    /// <summary>
    /// Place mock repository
    /// </summary>
    public class PlaceRepository : Repository.RedisRepository.PlaceRepository
    {
        private readonly ConcurrentDictionary<string, Place> data = new ConcurrentDictionary<string, Place>();
        private readonly ConcurrentDictionary<string, PlaceProduct> dataPlaceProduct = new ConcurrentDictionary<string, PlaceProduct>();
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        public PlaceRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.PlaceRepository>(), redisCacheClient)
        {

        }
        /// <summary>
        /// Set place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public override async Task<Place> SetPlace(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }

            data[place.Id] = place;
            return place;
        }
        /// <summary>
        /// Get place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public override async Task<Place> GetPlace(string placeId)
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
        public override async Task<IEnumerable<Place>> ListAll()
        {
            return data.Values;
        }
        /// <summary>
        /// Delete place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public virtual async Task DeletePlace(Place place)
        {
            if (place is null)
            {
                throw new ArgumentNullException(nameof(place));
            }
            data.TryRemove(place.Id, out var _);
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="placeProductid"></param>
        /// <returns></returns>
        public async override Task<PlaceProduct> GetPlaceProduct(string placeProductid)
        {
            return dataPlaceProduct[placeProductid];
        }
        /// <summary>
        /// Set
        /// </summary>
        /// <param name="placeProduct"></param>
        /// <returns></returns>
        public async override Task<PlaceProduct> SetProductPlace(PlaceProduct placeProduct)
        {
            dataPlaceProduct[placeProduct.Id] = placeProduct;
            return placeProduct;
        }
        /// <summary>
        /// DeleteProductPlace
        /// </summary>
        /// <param name="placeProductid"></param>
        /// <returns></returns>
        public async override Task<bool> DeletePlaceProduct(string placeProductid)
        {
            dataPlaceProduct.TryRemove(placeProductid, out var removed);
            if (removed == null) return false;
            return true;
        }
        /// <summary>
        /// Deletes all data
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = data.Count;
            data.Clear();
            dataPlaceProduct.Clear();
            return ret;
        }
    }
}
