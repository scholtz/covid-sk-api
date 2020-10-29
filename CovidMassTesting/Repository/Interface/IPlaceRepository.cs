using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    public interface IPlaceRepository
    {
        public Task DecrementPlaceRegistrations(string placeId);
        public Task IncrementPlaceRegistrations(string placeId);
        public Task IncrementPlaceHealthy(string placeId);
        public Task IncrementPlaceSick(string placeId);
        public Task<Place> GetPlace(string placeId);
        public Task<IEnumerable<Place>> ListAll();
        public Task<Place> Set(Place place);
        public Task Delete(Place place);
    }
}
