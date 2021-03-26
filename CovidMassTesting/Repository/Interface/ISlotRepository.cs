using CovidMassTesting.Model;
using CovidMassTesting.Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    /// <summary>
    /// Slot repositotory interface
    /// </summary>
    public interface ISlotRepository
    {
        /// <summary>
        /// Add 1 day slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Task<bool> Add(Slot1Day slot);
        /// <summary>
        /// Add 1 hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Task<bool> Add(Slot1Hour slot);
        /// <summary>
        /// Add 5 min slot
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public Task<bool> Add(Slot5Min slot);
        /// <summary>
        /// Decrement registration
        /// </summary>
        /// <param name="slotD"></param>
        /// <returns></returns>
        public Task<long> DecrementRegistrationDaySlot(Slot1Day slotD);
        /// <summary>
        /// Decrement registration
        /// </summary>
        /// <param name="slotH"></param>
        /// <returns></returns>
        public Task<long> DecrementRegistrationHourSlot(Slot1Hour slotH);
        /// <summary>
        /// Decrement registration
        /// </summary>
        /// <param name="slotM"></param>
        /// <returns></returns>
        public Task<long> DecrementRegistration5MinSlot(Slot5Min slotM);
        /// <summary>
        /// Increment registration
        /// </summary>
        /// <param name="slotD"></param>
        /// <returns></returns>
        public Task<long> IncrementRegistrationDaySlot(Slot1Day slotD);
        /// <summary>
        /// Increment registration
        /// </summary>
        /// <param name="slotH"></param>
        /// <returns></returns>
        public Task<long> IncrementRegistrationHourSlot(Slot1Hour slotH);
        /// <summary>
        /// Increment registration
        /// </summary>
        /// <param name="slotM"></param>
        /// <returns></returns>
        public Task<long> IncrementRegistration5MinSlot(Slot5Min slotM);
        /// <summary>
        /// Get day slot
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <returns></returns>
        public Task<Slot1Day> GetDaySlot(string placeId, long daySlotId);
        /// <summary>
        /// List day slots by place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<IEnumerable<Slot1Day>> ListDaySlotsByPlace(string placeId);
        /// <summary>
        /// Get hour slot
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <returns></returns>
        public Task<Slot1Hour> GetHourSlot(string placeId, long hourSlotId);
        /// <summary>
        /// List hour slots by place and day
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <returns></returns>
        public Task<IEnumerable<Slot1Hour>> ListHourSlotsByPlaceAndDaySlotId(string placeId, long daySlotId);
        /// <summary>
        /// Get 5 min slot
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="minuteSlotId"></param>
        /// <returns></returns>
        public Task<Slot5Min> Get5MinSlot(string placeId, long minuteSlotId);
        /// <summary>
        /// List minute slots by hour and place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <returns></returns>
        public Task<IEnumerable<Slot5Min>> ListMinuteSlotsByPlaceAndHourSlotId(string placeId, long hourSlotId);
        /// <summary>
        /// Checks slots by admin and creates them if any for any place is missing
        /// </summary>
        /// <param name="testingDay"></param>
        /// <param name="placeId"></param>
        /// <param name="openingHours"></param>
        /// <param name="openingHoursTemplate"></param>
        /// <returns></returns>
        public Task<int> CheckSlots(long testingDay, string placeId, string openingHours = "09:00-20:00", int openingHoursTemplate = 0);
        /// <summary>
        /// Get current slot
        /// </summary>
        /// <param name="place">Place id</param>
        /// <param name="time">Time when the registration should be done</param>
        /// <returns></returns>
        public Task<Slot5Min> GetCurrentSlot(string place, DateTimeOffset time);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
        /// <summary>
        /// Increment Slot Stats
        /// </summary>
        /// <param name="statsType"></param>
        /// <param name="slotType"></param>
        /// <param name="placeId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task<long> IncrementStats(StatsType.Enum statsType, SlotType.Enum slotType, string placeId, DateTimeOffset time);
        /// <summary>
        /// Decrement slot stats
        /// </summary>
        /// <param name="statsType"></param>
        /// <param name="slotType"></param>
        /// <param name="placeId"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public Task<long> DecrementStats(StatsType.Enum statsType, SlotType.Enum slotType, string placeId, DateTimeOffset time);
        /// <summary>
        /// DropAllStats
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public Task<bool> DropAllStats(DateTimeOffset? from);
        /// <summary>
        /// Fix slots
        /// </summary>
        /// <returns></returns>
        public Task<int> FixAllSlots();
        /// <summary>
        /// Set hour slot
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="newOnly"></param>
        public Task<bool> SetHourSlot(Slot1Hour slot, bool newOnly);
    }
}
