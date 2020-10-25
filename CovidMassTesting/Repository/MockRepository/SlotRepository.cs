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
    public class SlotRepository : Repository.RedisRepository.SlotRepository
    {
        private ConcurrentDictionary<string, Slot1Day> dataD = new ConcurrentDictionary<string, Slot1Day>();
        private ConcurrentDictionary<string, Slot1Hour> dataH = new ConcurrentDictionary<string, Slot1Hour>();
        private ConcurrentDictionary<string, Slot5Min> dataM = new ConcurrentDictionary<string, Slot5Min>();

        public SlotRepository(
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(loggerFactory.CreateLogger<Repository.RedisRepository.SlotRepository>(), redisCacheClient)
        {
        }
        public override async Task<bool> Add(Slot1Day slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            dataD[$"{slot.PlaceId}_{slot.Time.Ticks}"] = slot;
            return true;
        }
        public override async Task<bool> Add(Slot1Hour slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            dataH[$"{slot.PlaceId}_{slot.Time.Ticks}"] = slot;
            return true;
        }
        public override async Task<bool> Add(Slot5Min slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            dataM[$"{slot.PlaceId}_{slot.Time.Ticks}"] = slot;
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
        public override async Task IncrementRegistration5MinSlot(Slot5Min slotM)
        {
            dataM[$"{slotM.PlaceId}_{slotM.Time.Ticks}"].Registrations++;
        }
        public override async Task IncrementRegistrationDaySlot(Slot1Day slotD)
        {
            dataD[$"{slotD.PlaceId}_{slotD.Time.Ticks}"].Registrations++;
        }
        public override async Task IncrementRegistrationHourSlot(Slot1Hour slotH)
        {
            dataH[$"{slotH.PlaceId}_{slotH.Time.Ticks}"].Registrations++;
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
