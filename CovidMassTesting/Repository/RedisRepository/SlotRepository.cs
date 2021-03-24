using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    /// <summary>
    /// Slot repository manages db of time slots for sampling places
    /// </summary>
    public class SlotRepository : ISlotRepository
    {
        private readonly IStringLocalizer<SlotRepository> localizer;
        private readonly ILogger<SlotRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_SLOT_OBJECTS_D = "SLOTS_D";
        private readonly string REDIS_KEY_SLOT_OBJECTS_D_BY_PLACE = "SLOTS_D_P_LIST";

        private readonly string REDIS_KEY_SLOT_OBJECTS_H = "SLOTS_H";
        private readonly string REDIS_KEY_SLOT_OBJECTS_H_BY_PLACE_AND_DAY = "SLOTS_H_DP_LIST";

        private readonly string REDIS_KEY_SLOT_OBJECTS_M = "SLOTS_M";
        private readonly string REDIS_KEY_SLOT_OBJECTS_M_BY_PLACE_AND_HOUR = "SLOTS_M_DP_LIST";
        private readonly TimeSpan TimeZoneOffset = new TimeSpan(1, 0, 0);


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        public SlotRepository(
            IStringLocalizer<SlotRepository> localizer,
            IConfiguration configuration,
            ILogger<SlotRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.configuration = configuration;
            this.redisCacheClient = redisCacheClient;
        }
        /// <summary>
        /// Checks if all slots for the day, and testing place has been created
        /// </summary>
        /// <returns></returns>
        public async Task<int> CheckSlots(long testingDay, string placeId, string openingHours = "09:00-20:00", int openingHoursTemplate = 0)
        {
            var ret = 0;
            var day = new DateTimeOffset(testingDay, TimeSpan.Zero);
            var list = await ListDaySlotsByPlace(placeId);
            if (string.IsNullOrEmpty(openingHours))
            {
                //delete place
                var dayslot = list.FirstOrDefault(d => d.Time.Ticks == testingDay);
                if (dayslot != null)
                {
                    await DeleteDaySlot(dayslot);
                }
            }
            else
            {
                if (!list.Any(d => d.Time.Ticks == testingDay))
                {
                    ret++;
                    var result = await Add(new Slot1Day()
                    {
                        PlaceId = placeId,
                        Registrations = 0,
                        Time = day,
                        OpeningHours = openingHours,
                        OpeningHoursTemplate = openingHoursTemplate,
                        Description = day.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)
                    });
                    if (!result)
                    {
                        throw new Exception(localizer[Repository_RedisRepository_SlotRepository.Error_adding_the_slot_for_day].Value);
                    }
                }
                else
                {

                }
            }

            var start = day - day.AddHours(12).GetLocalOffset();
            var startInZone = start.ToLocalTime();
            var end = start.AddDays(1);
            var iterator = TimeSpan.Zero;

            var listH = (await ListHourSlotsByPlaceAndDaySlotId(placeId, testingDay)).ToDictionary(t => t.Time.Ticks, t => t);
            var hoursParsed = openingHours.ParseOpeningHours();
            var t = start + iterator;
            while (t < end)
            {
                var tNext = t.AddHours(1);
                if (hoursParsed.HasAnySlotWithinHourOpen(t.ToLocalTime().Hour))
                {
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
                            Description = $"{t.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(tNext.ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}"
                        });
                    }
                }
                else
                {
                    // remove slot
                    if (listH.ContainsKey(t.Ticks))
                    {
                        ret++;
                        await DeleteHourSlot(listH[t.Ticks]);
                    }
                }

                iterator = iterator.Add(TimeSpan.FromHours(1));
                t = start + iterator;
            }

            iterator = TimeSpan.Zero;
            var tMin = start + iterator;
            var tMinLocal = tMin.ToLocalTime();
            t = tMin;
            var tMinNext = tMin.AddMinutes(5);
            var listM = (await ListMinuteSlotsByPlaceAndHourSlotId(placeId, t.Ticks)).ToDictionary(t => t.Time.Ticks, t => t);
            var lastH = tMin.Hour;
            while (tMin < end)
            {
                tMinNext = tMin.AddMinutes(5);

                if (lastH != tMin.Hour)
                {
                    listM = (await ListMinuteSlotsByPlaceAndHourSlotId(placeId, tMin.Ticks)).ToDictionary(t => t.Time.Ticks, t => t);
                    t = tMin;
                    lastH = tMin.Hour;
                }
                var timeInSpan = new TimeSpan(tMinLocal.Hour, tMinLocal.Minute, 0);
                if (hoursParsed.IsTimeWhenIsOpen(timeInSpan))
                {
                    if (!listM.ContainsKey(tMin.Ticks))
                    {
                        ret++;
                        await Add(new Slot5Min()
                        {
                            PlaceId = placeId,
                            Registrations = 0,
                            Time = tMin,
                            TestingDayId = testingDay,
                            HourSlotId = t.Ticks,
                            Description = $"{(tMinLocal).ToString("HH:mm", CultureInfo.CurrentCulture)} - {(tMinNext.ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}"
                        });
                    }
                }
                else
                {
                    // remove slot
                    if (listM.ContainsKey(tMin.Ticks))
                    {
                        ret++;
                        await DeleteMinuteSlot(listM[tMin.Ticks]);
                    }
                }

                iterator = iterator.Add(TimeSpan.FromMinutes(5));
                tMin = start + iterator;
                tMinLocal = tMin.ToLocalTime();
            }

            return ret;
        }
        /// <summary>
        /// Increment registration for day slot
        /// </summary>
        /// <param name="slotD"></param>
        /// <returns></returns>
        public async Task IncrementRegistrationDaySlot(Slot1Day slotD)
        {
            if (slotD is null)
            {
                throw new ArgumentNullException(nameof(slotD));
            }

            var update = await GetDaySlot(slotD.PlaceId, slotD.Time.Ticks);
            update.Registrations++;
            await SetDaySlot(update, false);
        }
        /// <summary>
        /// Increment registrations for hour slot
        /// </summary>
        /// <param name="slotH"></param>
        /// <returns></returns>
        public async Task IncrementRegistrationHourSlot(Slot1Hour slotH)
        {
            if (slotH is null)
            {
                throw new ArgumentNullException(nameof(slotH));
            }

            var update = await GetHourSlot(slotH.PlaceId, slotH.Time.Ticks);
            update.Registrations++;
            await SetHourSlot(update, false);
        }
        /// <summary>
        /// Increment registrations for minute slot
        /// </summary>
        /// <param name="slotM"></param>
        /// <returns></returns>
        public async Task IncrementRegistration5MinSlot(Slot5Min slotM)
        {
            if (slotM is null)
            {
                throw new ArgumentNullException(nameof(slotM));
            }

            var update = await Get5MinSlot(slotM.PlaceId, slotM.Time.Ticks);
            update.Registrations++;
            await SetMinuteSlot(update, false);
        }
        /// <summary>
        /// Decrement registrations for day slot
        /// </summary>
        /// <param name="slotD"></param>
        /// <returns></returns>
        public async Task DecrementRegistrationDaySlot(Slot1Day slotD)
        {
            if (slotD is null)
            {
                throw new ArgumentNullException(nameof(slotD));
            }

            var update = await GetDaySlot(slotD.PlaceId, slotD.Time.Ticks);
            update.Registrations--;
            await SetDaySlot(update, false);
        }
        /// <summary>
        /// Decrement registrations for hour slot
        /// </summary>
        /// <param name="slotH"></param>
        /// <returns></returns>
        public async Task DecrementRegistrationHourSlot(Slot1Hour slotH)
        {
            if (slotH is null)
            {
                throw new ArgumentNullException(nameof(slotH));
            }

            var update = await GetHourSlot(slotH.PlaceId, slotH.Time.Ticks);
            update.Registrations--;
            await SetHourSlot(update, false);
        }
        /// <summary>
        /// Decrement registrations for minute slot
        /// </summary>
        /// <param name="slotM"></param>
        /// <returns></returns>
        public async Task DecrementRegistration5MinSlot(Slot5Min slotM)
        {
            if (slotM is null)
            {
                throw new ArgumentNullException(nameof(slotM));
            }

            var update = await Get5MinSlot(slotM.PlaceId, slotM.Time.Ticks);
            update.Registrations--;
            await SetMinuteSlot(update, false);
        }
        /// <summary>
        /// Create day slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Task<bool> Add(Slot1Day slot)
        {
            return SetDaySlot(slot, true);
        }
        /// <summary>
        /// Create hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Task<bool> Add(Slot1Hour slot)
        {
            return SetHourSlot(slot, true);
        }
        /// <summary>
        /// Create minute slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Task<bool> Add(Slot5Min slot)
        {
            return SetMinuteSlot(slot, true);
        }

        /// <summary>
        /// Updates day slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        /// <returns></returns>
        public virtual async Task<bool> SetDaySlot(Slot1Day slot, bool newOnly)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            try
            {
                var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{slot.PlaceId}_{slot.Time.Ticks}", slot, newOnly);
                if (newOnly && !ret)
                {
                    throw new Exception(localizer[Repository_RedisRepository_SlotRepository.Error_creating_day_slot].Value);
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
        /// <summary>
        /// Updates hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        /// <returns></returns>
        public virtual async Task<bool> SetHourSlot(Slot1Hour slot, bool newOnly)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            try
            {
                var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{slot.PlaceId}_{slot.Time.Ticks}", slot, newOnly);
                if (newOnly && !ret)
                {
                    throw new Exception(localizer[Repository_RedisRepository_SlotRepository.Error_creating_hour_slot].Value);
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
        /// <summary>
        /// Updates minute slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        /// <returns></returns>
        public virtual async Task<bool> SetMinuteSlot(Slot5Min slot, bool newOnly)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            try
            {
                var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{slot.PlaceId}_{slot.Time.Ticks}", slot, newOnly);
                if (newOnly && !ret)
                {
                    throw new Exception(localizer[Repository_RedisRepository_SlotRepository.Error_creating_minute_slot].Value);
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



        /// <summary>
        /// Deletes day slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteDaySlot(Slot1Day slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            try
            {
                var ret = await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }
        /// <summary>
        /// Deletes hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteHourSlot(Slot1Hour slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            try
            {
                var ret = await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }
        /// <summary>
        /// Updates minute slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public virtual async Task<bool> DeleteMinuteSlot(Slot5Min slot)
        {
            if (slot is null)
            {
                throw new ArgumentNullException(nameof(slot));
            }

            try
            {
                var ret = await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }

        /// <summary>
        /// Returns day slot
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <returns></returns>
        public virtual Task<Slot1Day> GetDaySlot(string placeId, long daySlotId)
        {
            return redisCacheClient.Db0.HashGetAsync<Slot1Day>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{placeId}_{daySlotId}");
        }
        /// <summary>
        /// List day slots by place id
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Slot1Day>> ListDaySlotsByPlace(string placeId)
        {
            var ret = new List<Slot1Day>();
            foreach (var slot in (await redisCacheClient.Db0.SetMembersAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D_BY_PLACE}_{placeId}")))
            {
                var daySlot = await redisCacheClient.Db0.HashGetAsync<Slot1Day>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", slot);
                if (daySlot != null)
                {
                    ret.Add(daySlot);
                }
            }
            return ret;
        }
        /// <summary>
        /// List day slot
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <returns></returns>
        public virtual Task<Slot1Hour> GetHourSlot(string placeId, long hourSlotId)
        {
            return redisCacheClient.Db0.HashGetAsync<Slot1Hour>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{placeId}_{hourSlotId}");
        }
        /// <summary>
        /// Lists hour slots by place and day
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Slot1Hour>> ListHourSlotsByPlaceAndDaySlotId(string placeId, long daySlotId)
        {
            var ret = new List<Slot1Hour>();
            foreach (var slot in await redisCacheClient.Db0.SetMembersAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H_BY_PLACE_AND_DAY}_{placeId}_{daySlotId}"))
            {
                var hourSlot = await redisCacheClient.Db0.HashGetAsync<Slot1Hour>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", slot);
                if (hourSlot != null)
                {
                    ret.Add(hourSlot);
                }
            }
            return ret;
        }
        /// <summary>
        /// Loads minute slot
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="minuteSlotId"></param>
        /// <returns></returns>
        public virtual Task<Slot5Min> Get5MinSlot(string placeId, long minuteSlotId)
        {
            return redisCacheClient.Db0.HashGetAsync<Slot5Min>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{placeId}_{minuteSlotId}");
        }
        /// <summary>
        /// List all minute slots by hour and place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Slot5Min>> ListMinuteSlotsByPlaceAndHourSlotId(string placeId, long hourSlotId)
        {
            var ret = new List<Slot5Min>();
            foreach (var slot in await redisCacheClient.Db0.SetMembersAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M_BY_PLACE_AND_HOUR}_{placeId}_{hourSlotId}"))
            {
                var minSlot = await redisCacheClient.Db0.HashGetAsync<Slot5Min>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", slot);
                if (minSlot != null)
                {
                    ret.Add(minSlot);
                }
            }
            return ret;
        }
        /// <summary>
        /// Get current slot
        /// </summary>
        /// <param name="place"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public async Task<Slot5Min> GetCurrentSlot(string place, DateTimeOffset time)
        {
            var days = await ListDaySlotsByPlace(place);
            var currentDay = days.Where(d => d.SlotId < time.Ticks).OrderByDescending(d => d.SlotId).FirstOrDefault();
            if (currentDay == null)
            {
                throw new Exception("Toto miesto dnes nie je otvorené");
            }

            var hours = await ListHourSlotsByPlaceAndDaySlotId(place, currentDay.SlotId);
            if (hours == null)
            {
                throw new Exception("Toto miesto dnes nie je otvorené");
            }

            var currentHour = hours.Where(d => d.SlotId < time.Ticks).OrderByDescending(d => d.SlotId).FirstOrDefault();
            if (currentHour == null)
            {
                currentHour = hours.Last();
            }

            var minutes = await ListMinuteSlotsByPlaceAndHourSlotId(place, currentHour.SlotId);
            var ret = minutes.Where(d => d.SlotId < time.Ticks).OrderByDescending(d => d.SlotId).FirstOrDefault();
            if (ret == null)
            {
                return minutes.Last();
            }

            return ret;
        }
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> DropAllData()
        {
            var ret = 0;

            foreach (var slot in await redisCacheClient.Db0.HashValuesAsync<Slot1Day>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}"))
            {
                try
                {
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_D_BY_PLACE}_{slot.PlaceId}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                    ret++;
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Error while dropping slot data: {exc.Message}");
                }
            }
            foreach (var slot in await redisCacheClient.Db0.HashValuesAsync<Slot1Hour>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}"))
            {
                try
                {
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_H_BY_PLACE_AND_DAY}_{slot.PlaceId}_{slot.DaySlotId}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                    ret++;

                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Error while dropping slot data: {exc.Message}");
                }
            }
            foreach (var slot in await redisCacheClient.Db0.HashValuesAsync<Slot5Min>($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}"))
            {
                try
                {
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_SLOT_OBJECTS_M_BY_PLACE_AND_HOUR}_{slot.PlaceId}_{slot.HourSlotId}", $"{slot.PlaceId}_{slot.Time.Ticks}");
                    ret++;
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Error while dropping slot data: {exc.Message}");
                }
            }
            return ret;
        }
    }
}
