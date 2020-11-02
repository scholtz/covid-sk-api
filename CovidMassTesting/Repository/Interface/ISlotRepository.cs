using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    public interface ISlotRepository
    {
        public Task<bool> Add(Slot1Day slot);
        public Task<bool> Add(Slot1Hour slot);
        public Task<bool> Add(Slot5Min slot);
        public Task DecrementRegistrationDaySlot(Slot1Day slotD);
        public Task DecrementRegistrationHourSlot(Slot1Hour slotH);
        public Task DecrementRegistration5MinSlot(Slot5Min slotM);
        public Task IncrementRegistrationDaySlot(Slot1Day slotD);
        public Task IncrementRegistrationHourSlot(Slot1Hour slotH);
        public Task IncrementRegistration5MinSlot(Slot5Min slotM);
        public Task<Slot1Day> GetDaySlot(string placeId, long daySlotId);
        public Task<IEnumerable<Slot1Day>> ListDaySlotsByPlace(string placeId);
        public Task<Slot1Hour> GetHourSlot(string placeId, long hourSlotId);
        public Task<IEnumerable<Slot1Hour>> ListHourSlotsByPlaceAndDaySlotId(string placeId, long daySlotId);
        public Task<Slot5Min> Get5MinSlot(string placeId, long minuteSlotId);
        public Task<IEnumerable<Slot5Min>> ListMinuteSlotsByPlaceAndHourSlotId(string placeId, long hourSlotId);
        public Task<int> CheckSlots(long testingDay, string placeId, int testingFromHour = 9, int testingUntilHour = 20);
        public Task<Slot5Min> GetCurrentSlot(string place);
    }
}
