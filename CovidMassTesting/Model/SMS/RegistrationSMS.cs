using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// Registration SMS
    /// </summary>
    public class RegistrationSMS : ISMS
    {
        /// <summary>
        /// Person name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Code
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Date
        /// </summary>
        public string Date { get; set; }
        /// <summary>
        /// Place
        /// </summary>
        public string Place { get; set; }
        /// <summary>
        /// Generate sms message text
        /// </summary>
        /// <returns></returns>
        public override string GetText()
        {
            return $"Kód registrácie: {Code}. {Name}, úspešne ste sa zaregistrovali k odberu vzorky k termínu {Date} na odberné miesto {Place}";
        }
    }
}
