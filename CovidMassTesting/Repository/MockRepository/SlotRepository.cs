using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Model.Enums;
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
        private readonly ConcurrentDictionary<string, long> Stats = new ConcurrentDictionary<string, long>();
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
        /// Deletes single day slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public async override Task<bool> DeleteDaySlot(Slot1Day slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";
            if (dataD.ContainsKey(key))
            {
                dataD.TryRemove(key, out var _);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Deletes single hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public async override Task<bool> DeleteHourSlot(Slot1Hour slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";
            if (dataH.ContainsKey(key))
            {
                dataH.TryRemove(key, out var _);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Deletes single minute slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public async override Task<bool> DeleteMinuteSlot(Slot5Min slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";
            if (dataM.ContainsKey(key))
            {
                dataM.TryRemove(key, out var _);
                return true;
            }
            return false;
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
            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";
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
            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";
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
        /// Remove hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public override async Task<bool> RemoveSlotH(Slot1Hour slot)
        {
            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";

            if (!dataH.ContainsKey(key))
            {
                throw new Exception("Item already exists");
            }
            dataH.TryRemove(key, out var _);
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
            string key = $"{slot.PlaceId}_{slot.Time.UtcTicks}";
            if (newOnly)
            {
                if (dataM.ContainsKey(key))
                {
                    throw new Exception("Item already exists");
                }
            }
            dataM[key] = slot;
            await Task.Delay(1);
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
        /// <summary>
        /// Increment stats
        /// </summary>
        /// <param name="statsType"></param>
        /// <param name="slotType"></param>
        /// <param name="placeId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public async override Task<long> IncrementStats(StatsType.Enum statsType, SlotType.Enum slotType, string placeId, DateTimeOffset time)
        {
            var t = slotType switch
            {
                SlotType.Enum.Day => time.RoundDay(),
                SlotType.Enum.Hour => time.RoundHour(),
                SlotType.Enum.Min => time.RoundMinute(),
                _ => throw new Exception("Invalid slot type"),
            };
            var keyPlace = $"{StatsType.ToText(statsType)}-slot-{SlotType.ToText(slotType)}-{placeId}-{t}";
            if (!Stats.ContainsKey(keyPlace)) Stats[keyPlace] = 0;
            Stats[keyPlace]++;
            return Stats[keyPlace];
        }
        /// <summary>
        /// Decrement stats
        /// </summary>
        /// <param name="statsType"></param>
        /// <param name="slotType"></param>
        /// <param name="placeId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public async override Task<long> DecrementStats(StatsType.Enum statsType, SlotType.Enum slotType, string placeId, DateTimeOffset time)
        {

            var t = slotType switch
            {
                SlotType.Enum.Day => time.RoundDay(),
                SlotType.Enum.Hour => time.RoundHour(),
                SlotType.Enum.Min => time.RoundMinute(),
                _ => throw new Exception("Invalid slot type"),
            };
            var keyPlace = $"{StatsType.ToText(statsType)}-slot-{SlotType.ToText(slotType)}-{placeId}-{t}";
            if (!Stats.ContainsKey(keyPlace)) Stats[keyPlace] = 0;
            Stats[keyPlace]--;
            return Stats[keyPlace];
        }
        /// <summary>
        /// Drop stats from time or all
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public async override Task<bool> DropAllStats(DateTimeOffset? from)
        {

            if (from.HasValue)
            {
                var decisionTick = from.Value.UtcTicks;
                var toRemove = Stats.Keys.Where(item =>
                {

                    var k = item.Split("-");
                    if (k.Length > 4)
                    {
                        if (long.TryParse(k[k.Length - 1], out var time))
                        {
                            if (time >= decisionTick)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }).ToArray();
                foreach (var item in toRemove)
                {
                    Stats.TryRemove(item, out var _);
                }
            }
            else
            {
                Stats.Clear();
            }
            return true;
        }
        /// <summary>
        /// Return stats for specific place and time
        /// </summary>
        /// <param name="statsType"></param>
        /// <param name="slotType"></param>
        /// <param name="placeId"></param>
        /// <param name="slotId"></param>
        /// <returns></returns>
        public async override Task<long?> GetStats(StatsType.Enum statsType, SlotType.Enum slotType, string placeId, long slotId)
        {
            var keyPlace = $"{StatsType.ToText(statsType)}-slot-{SlotType.ToText(slotType)}-{placeId}-{slotId}";
            if (Stats.ContainsKey(keyPlace))
            {
                return Stats[keyPlace];
            }
            return 0;
        }
        /// <summary>
        /// Day slots keys
        /// </summary>
        /// <returns></returns>
        public async override Task<IEnumerable<string>> GetSlotKeysD()
        {
            return dataD.Keys;
        }
        /// <summary>
        /// Hour slots keys
        /// </summary>
        /// <returns></returns>
        public async override Task<IEnumerable<string>> GetSlotKeysH()
        {
            return dataH.Keys;
        }
        /// <summary>
        /// Minute slots keys
        /// </summary>
        /// <returns></returns>
        public async override Task<IEnumerable<string>> GetSlotKeysM()
        {
            return dataM.Keys;
        }


    }
}
