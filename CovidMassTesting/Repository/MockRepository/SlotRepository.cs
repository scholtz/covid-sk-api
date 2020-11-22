using CovidMassTesting.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
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
    /// Slot mock repository
    /// </summary>
    public class SlotRepository : Repository.RedisRepository.SlotRepository
    {
        private readonly ConcurrentDictionary<string, Slot1Day> dataD = new ConcurrentDictionary<string, Slot1Day>();
        private readonly ConcurrentDictionary<string, Slot1Hour> dataH = new ConcurrentDictionary<string, Slot1Hour>();
        private readonly ConcurrentDictionary<string, Slot5Min> dataM = new ConcurrentDictionary<string, Slot5Min>();
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        public SlotRepository(
            IStringLocalizer<Repository.RedisRepository.SlotRepository> localizer,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(localizer,
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.SlotRepository>(),
                redisCacheClient
            )
        {
        }
        /// <summary>
        /// Set
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        /// <returns></returns>
        public override async Task<bool> SetDaySlot(Slot1Day slot, bool newOnly)
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
        /// <summary>
        /// Set hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        /// <returns></returns>
        public override async Task<bool> SetHourSlot(Slot1Hour slot, bool newOnly)
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
        /// <summary>
        /// Set minute slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        /// <returns></returns>
        public override async Task<bool> SetMinuteSlot(Slot5Min slot, bool newOnly)
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
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="minuteSlotId"></param>
        /// <returns></returns>
        public override async Task<Slot5Min> Get5MinSlot(string placeId, long minuteSlotId)
        {
            return dataM[$"{placeId}_{minuteSlotId}"];
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <returns></returns>
        public override async Task<Slot1Day> GetDaySlot(string placeId, long daySlotId)
        {
            return dataD[$"{placeId}_{daySlotId}"];
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <returns></returns>
        public override async Task<Slot1Hour> GetHourSlot(string placeId, long hourSlotId)
        {
            return dataH[$"{placeId}_{hourSlotId}"];
        }
        /// <summary>
        /// List
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<Slot1Day>> ListDaySlotsByPlace(string placeId)
        {
            return dataD.Values.Where(s => s.PlaceId == placeId);
        }
        /// <summary>
        /// List
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<Slot1Hour>> ListHourSlotsByPlaceAndDaySlotId(string placeId, long daySlotId)
        {
            return dataH.Values.Where(s => s.PlaceId == placeId && s.DaySlotId == daySlotId);
        }
        /// <summary>
        /// List
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <returns></returns>
        public override async Task<IEnumerable<Slot5Min>> ListMinuteSlotsByPlaceAndHourSlotId(string placeId, long hourSlotId)
        {
            return dataM.Values.Where(s => s.PlaceId == placeId && s.HourSlotId == hourSlotId);
        }
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = dataD.Count + dataH.Count + dataM.Count;
            dataD.Clear();
            dataH.Clear();
            dataM.Clear();
            return ret;
        }
    }
}
