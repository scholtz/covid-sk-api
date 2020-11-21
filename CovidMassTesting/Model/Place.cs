using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// This object holds all information about the testing place plus real time statistics about health/sick visitors
    /// </summary>
    public class Place
    {
        /// <summary>
        /// Place id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Place name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Registration in minute slot is rejected if more then this amount of people are registered
        /// </summary>
        public int LimitPer5MinSlot { get; set; } = 5;
        /// <summary>
        /// Registration in hour slot is rejected if more then this amount of people are registered
        /// </summary>
        public int LimitPer1HourSlot { get; set; } = 40;
        /// <summary>
        /// Description 
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// GPS lattitude
        /// </summary>
        public decimal Lat { get; set; }
        /// <summary>
        /// GPS longtitude
        /// </summary>
        public decimal Lng { get; set; }
        /// <summary>
        /// Has drive in option
        /// </summary>
        public bool IsDriveIn { get; set; }
        /// <summary>
        /// Has walk in option
        /// </summary>
        public bool IsWalkIn { get; set; }
        /// <summary>
        /// Total number of registrations
        /// </summary>
        public int Registrations { get; set; }
        /// <summary>
        /// Results of healthy results
        /// </summary>
        public int Healthy { get; set; }
        /// <summary>
        /// Results of covid positive results
        /// </summary>
        public int Sick { get; set; }

    }
}
