using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    public class SlotRepository : ISlotRepository
    {
        private readonly ILogger<SlotRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_SLOT_OBJECTS_D = "SLOTS_D";
        private readonly string REDIS_KEY_SLOT_OBJECTS_D_BY_PLACE = "SLOTS_D_P_LIST";

        private readonly string REDIS_KEY_SLOT_OBJECTS_H = "SLOTS_H";
        private readonly string REDIS_KEY_SLOT_OBJECTS_H_BY_PLACE_AND_DAY = "SLOTS_H_DP_LIST";

        private readonly string REDIS_KEY_SLOT_OBJECTS_M = "SLOTS_M";
        private readonly string REDIS_KEY_SLOT_OBJECTS_M_BY_PLACE_AND_HOUR = "SLOTS_M_DP_LIST";


        public SlotRepository(
            IConfiguration configuration,
            ILogger<SlotRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.logger = logger;
            this.configuration = configuration;
            this.redisCacheClient = redisCacheClient;
        }
        /// <summary>
        /// Checks if all slots for the day, and testing place has been created
        /// </summary>
        /// <returns></returns>
        public async Task<int> CheckSlots(long testingDay, string placeId, int testingFromHour = 9, int testingUntilHour = 20)
        {
            int ret = 0;
            var day = new DateTimeOffset(testingDay, TimeSpan.Zero);
            var list = await ListDaySlotsByPlace(placeId);
            if (!list.Any(d => d.Time.Ticks == testingDay))
            {
                ret++;
                var result = await Add(new Slot1Day()
                {
                    PlaceId = placeId,
                    Registrations = 0,
                    Time = day,
                    Description = (day + TimeZoneInfo.Local.GetUtcOffset(day)).ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)
                });
                if (!result)
                {
                    throw new Exception("Error adding the slot for day");
                }
            }


            var listH = (await ListHourSlotsByPlaceAndDaySlotId(placeId, testingDay)).ToDictionary(t => t.Time.Ticks, t => t);
            for (int hour = testingFromHour; hour < testingUntilHour; hour++)
            {
                var t = day + TimeSpan.FromHours(hour);
                var tNext = t.AddHours(1);
                if (!listH.ContainsKey(t.Ticks))
                {
                    ret++;
                    await Add(new Slot1Hour()
                    {
                        PlaceId = placeId,
                        Registrations = 0,
                        Time = t,
                        DaySlotId = day.Ticks,
                        TestingDayId = testingDay,
                        Description = $"{(t).ToString("HH:mm", CultureInfo.CurrentCulture)} - {(tNext).ToString("HH:mm", CultureInfo.CurrentCulture)}"
                    });
                }

                var listM = (await ListMinuteSlotsByPlaceAndHourSlotId(placeId, t.Ticks)).ToDictionary(t => t.Time.Ticks, t => t);
                for (int minute = 0; minute < 60; minute += 5)
                {
                    var tMin = day + TimeSpan.FromHours(hour) + TimeSpan.FromMinutes(minute);
                    var tMinNext = tMin.AddMinutes(5);
                    if (!listH.ContainsKey(tMin.Ticks))
                    {
                        ret++;
                        await Add(new Slot5Min()
                        {
                            PlaceId = placeId,
                            Registrations = 0,
                            Time = tMin,
                            TestingDayId = testingDay,
                            HourSlotId = t.Ticks,
                            Description = $"{(tMin).ToString("HH:mm", CultureInfo.CurrentCulture)} - {(tMinNext).ToString("HH:mm", CultureInfo.CurrentCulture)}"
                        });
                    }
                }
            }
            return ret;
        }

        public virtual async Task IncrementRegistrationDaySlot(Slot1Day slotD)
        {
            var update = await GetDaySlot(slotD.PlaceId, slotD.Time.Ticks);
            update.Registrations++;
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{slotD.PlaceId}_{slotD.Time.Ticks}", update);
        }
        public virtual async Task IncrementRegistrationHourSlot(Slot1Hour slotH)
        {
            var update = await GetHourSlot(slotH.PlaceId, slotH.Time.Ticks);
            update.Registrations++;
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{slotH.PlaceId}_{slotH.Time.Ticks}", update);
        }

        public virtual async Task IncrementRegistration5MinSlot(Slot5Min slotM)
        {
            var update = await Get5MinSlot(slotM.PlaceId, slotM.Time.Ticks);
            update.Registrations++;
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{slotM.PlaceId}_{slotM.Time.Ticks}", update);
        }

        public virtual async Task<bool> Add(Slot1Day slot)
        {
            try
            {
                if (!await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{slot.PlaceId}_{slot.Time.Ticks}", slot, true))
                {
                    throw new Exception("Error creating place");
                }
                await redisCacheClient.Db0.SetAddAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D_BY_PLACE}_{slot.PlaceId}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }
        public virtual async Task<bool> Add(Slot1Hour slot)
        {
            try
            {
                if (!await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{slot.PlaceId}_{slot.Time.Ticks}", slot, true))
                {
                    throw new Exception("Error creating place");
                }
                await redisCacheClient.Db0.SetAddAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H_BY_PLACE_AND_DAY}_{slot.PlaceId}_{slot.DaySlotId}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }
        public virtual async Task<bool> Add(Slot5Min slot)
        {
            try
            {
                if (!await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{slot.PlaceId}_{slot.Time.Ticks}", slot, true))
                {
                    throw new Exception("Error creating place");
                }
                await redisCacheClient.Db0.SetAddAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M_BY_PLACE_AND_HOUR}_{slot.PlaceId}_{slot.HourSlotId}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }

        public virtual Task<Slot1Day> GetDaySlot(string placeId, long daySlotId)
        {
            return redisCacheClient.Db0.HashGetAsync<Slot1Day>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{placeId}_{daySlotId}");
        }
        public virtual async Task<IEnumerable<Slot1Day>> ListDaySlotsByPlace(string placeId)
        {
            var ret = new List<Slot1Day>();
            foreach (var slot in (await redisCacheClient.Db0.SetMembersAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D_BY_PLACE}_{placeId}")))
            {
                ret.Add(await redisCacheClient.Db0.HashGetAsync<Slot1Day>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", slot));
            }
            return ret;
        }
        public virtual Task<Slot1Hour> GetHourSlot(string placeId, long hourSlotId)
        {
            return redisCacheClient.Db0.HashGetAsync<Slot1Hour>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{placeId}_{hourSlotId}");
        }
        public virtual async Task<IEnumerable<Slot1Hour>> ListHourSlotsByPlaceAndDaySlotId(string placeId, long daySlotId)
        {
            var ret = new List<Slot1Hour>();
            foreach (var slot in await redisCacheClient.Db0.SetMembersAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H_BY_PLACE_AND_DAY}_{placeId}_{daySlotId}"))
            {
                ret.Add(await redisCacheClient.Db0.HashGetAsync<Slot1Hour>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", slot));
            }
            return ret;
        }
        public virtual Task<Slot5Min> Get5MinSlot(string placeId, long minuteSlotId)
        {
            return redisCacheClient.Db0.HashGetAsync<Slot5Min>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{placeId}_{minuteSlotId}");
        }
        public virtual async Task<IEnumerable<Slot5Min>> ListMinuteSlotsByPlaceAndHourSlotId(string placeId, long hourSlotId)
        {
            var ret = new List<Slot5Min>();
            foreach (var slot in await redisCacheClient.Db0.SetMembersAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M_BY_PLACE_AND_HOUR}_{placeId}_{hourSlotId}"))
            {
                ret.Add(await redisCacheClient.Db0.HashGetAsync<Slot5Min>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", slot));
            }
            return ret;
        }
    }
}
