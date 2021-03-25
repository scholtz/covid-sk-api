using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Enums
{
    /// <summary>
    /// Stats types
    /// </summary>
    public class SlotType
    {
        /// <summary>
        /// Enum
        /// </summary>
        public enum Enum
        {
            /// <summary>
            /// Day slot
            /// </summary>
            Day,
            /// <summary>
            /// Hour slot
            /// </summary>
            Hour,
            /// <summary>
            /// 5 Minute slot
            /// </summary>
            Min
        }
        /// <summary>
        /// Enum to text
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ToText(SlotType.Enum type)
        {
            return type switch
            {
                SlotType.Enum.Day => SlotType.Day,
                SlotType.Enum.Hour => SlotType.Hour,
                SlotType.Enum.Min => SlotType.Min,
                _ => throw new Exception("Invalid slot type"),
            };
        }

        /// <summary>
        /// Day slot
        /// </summary>
        public const string Day = "day";
        /// <summary>
        /// Hour slot
        /// </summary>
        public const string Hour = "hour";
        /// <summary>
        /// 5 min slot
        /// </summary>
        public const string Min = "5min";
    }
}