using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class Place
    {
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// Registration in minute slot is rejected if more then this amount of people are registered
        /// </summary>
        public int LimitPer5MinSlot { get; set; } = 5;
        /// <summary>
        /// Registration in hour slot is rejected if more then this amount of people are registered
        /// </summary>
        public int LimitPer1HourSlot { get; set; } = 40;
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
        public bool IsDriveIn { get; set; }
        public bool IsWalkIn { get; set; }
        public int Registrations { get; set; }
        public int Healthy { get; set; }
        public int Sick { get; set; }

    }
}
