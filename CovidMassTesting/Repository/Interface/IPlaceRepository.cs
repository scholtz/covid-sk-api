using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    /// <summary>
    /// Place repository interface for dependency injection
    /// </summary>
    public interface IPlaceRepository
    {
        /// <summary>
        /// Decrement registrations at place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task DecrementPlaceRegistrations(string placeId);
        /// <summary>
        /// Increment registrations at place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task IncrementPlaceRegistrations(string placeId);
        /// <summary>
        /// increment healthy visitors
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task IncrementPlaceHealthy(string placeId);
        /// <summary>
        /// increment sick visitors
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task IncrementPlaceSick(string placeId);
        /// <summary>
        /// Returns place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<Place> GetPlace(string placeId);
        /// <summary>
        /// List all places
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Place>> ListAll();
        /// <summary>
        /// Set new place object
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public Task<Place> SetPlace(Place place);
        /// <summary>
        /// Admin can remove place
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        public Task Delete(Place place);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
        /// <summary>
        /// Create/Update product place
        /// </summary>
        /// <param name="placeProduct"></param>
        /// <returns></returns>
        public Task<PlaceProduct> SetProductPlace(PlaceProduct placeProduct);
        /// <summary>
        /// Delete product place
        /// </summary>
        /// <param name="placeProduct"></param>
        /// <returns></returns>
        public Task<bool> DeletePlaceProduct(PlaceProduct placeProduct);
        /// <summary>
        /// List by place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<List<PlaceProduct>> ListPlaceProductByPlace(string placeId);
        /// <summary>
        /// List by pp
        /// </summary>
        /// <param name="placeProvider"></param>
        /// <returns></returns>
        public Task<List<PlaceProduct>> ListPlaceProductByPlaceProvider(PlaceProvider placeProvider);
        /// <summary>
        /// Get product place
        /// </summary>
        /// <param name="placeProductid"></param>
        /// <returns></returns>
        public Task<PlaceProduct> GetPlaceProduct(string placeProductid);
    }
}
