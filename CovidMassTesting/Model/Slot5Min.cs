using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class Slot5Min
    {
        public long SlotId { get { return Time.Ticks; } }
        public string PlaceId { get; set; }
        public long TestingDayId { get; set; }
        public DateTimeOffset Time { get; set; }
        public long HourSlotId { get; set; }
        public string Description { get; set; }
        public int Registrations { get; set; }
    }
}
