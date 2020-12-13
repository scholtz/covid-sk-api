using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Currency validator
    /// </summary>
    public static class Currency
    {

        /// <summary>
        /// shared infrastructure
        /// </summary>
        public const string EUR = "EUR";
        /// <summary>
        /// bronze infrastructure
        /// </summary>
        public const string USD = "USD";
        /// <summary>
        /// silver infrastructure
        /// </summary>
        public const string CZK = "CZK";
        /// <summary>
        /// validates sla level
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool ValidateCurrency(this string input)
        {
            switch (input)
            {
                case EUR:
                case USD:
                case CZK:
                    return true;
            }
            return false;
        }
    }
}
