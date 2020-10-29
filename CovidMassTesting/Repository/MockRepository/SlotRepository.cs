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
    public class SlotRepository : Repository.RedisRepository.SlotRepository
    {
        private readonly ConcurrentDictionary<string, Slot1Day> dataD = new ConcurrentDictionary<string, Slot1Day>();
        private readonly ConcurrentDictionary<string, Slot1Hour> dataH = new ConcurrentDictionary<string, Slot1Hour>();
        private readonly ConcurrentDictionary<string, Slot5Min> dataM = new ConcurrentDictionary<string, Slot5Min>();

        public SlotRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.SlotRepository>(), redisCacheClient)
        {
        }
        public override async Task<bool> Set(Slot1Day slot, bool newOnly)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
            string key = $"{slot.PlaceId}_{slot.Time.Ticks}";
            if (newOnly)
            {
                if (dataD.ContainsKey(key))
                {
                    throw new Exception("Item already exists");
                }
            }
            dataD[key] = slot;
            return true;
        }
        public override async Task<bool> Set(Slot1Hour slot, bool newOnly)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
            string key = $"{slot.PlaceId}_{slot.Time.Ticks}";
            if (newOnly)
            {
                if (dataH.ContainsKey(key))
                {
                    throw new Exception("Item already exists");
                }
            }
            dataH[key] = slot;
            return true;
        }
        public override async Task<bool> Set(Slot5Min slot, bool newOnly)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }
            string key = $"{slot.PlaceId}_{slot.Time.Ticks}";
            if (newOnly)
            {
                if (dataM.ContainsKey(key))
                {
                    throw new Exception("Item already exists");
                }
            }
            dataM[key] = slot;
            return true;
        }
        public override async Task<Slot5Min> Get5MinSlot(string placeId, long minuteSlotId)
        {
            return dataM[$"{placeId}_{minuteSlotId}"];
        }
        public override async Task<Slot1Day> GetDaySlot(string placeId, long daySlotId)
        {
            return dataD[$"{placeId}_{daySlotId}"];
        }
        public override async Task<Slot1Hour> GetHourSlot(string placeId, long hourSlotId)
        {
            return dataH[$"{placeId}_{hourSlotId}"];
        }

        public override async Task<IEnumerable<Slot1Day>> ListDaySlotsByPlace(string placeId)
        {
            return dataD.Values.Where(s => s.PlaceId == placeId);
        }
        public override async Task<IEnumerable<Slot1Hour>> ListHourSlotsByPlaceAndDaySlotId(string placeId, long daySlotId)
        {
            return dataH.Values.Where(s => s.PlaceId == placeId && s.DaySlotId == daySlotId);
        }
        public override async Task<IEnumerable<Slot5Min>> ListMinuteSlotsByPlaceAndHourSlotId(string placeId, long hourSlotId)
        {
            return dataM.Values.Where(s => s.PlaceId == placeId && s.HourSlotId == hourSlotId);
        }

    }
}
