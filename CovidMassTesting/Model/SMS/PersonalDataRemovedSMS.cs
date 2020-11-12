using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// Notification after we remove personal data from system
    /// 
    /// Healthy people are allowed to remove their data from system.
    /// </summary>
    public class PersonalDataRemovedSMS : ISMS
    {
        /// <summary>
        /// Person name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Generate sms message text
        /// </summary>
        /// <returns></returns>
        public override string GetText()
        {
            return $"Dobrý deň {Name}. Vaše osobné údaje sme odstránili zo systému. Ďakujeme že ste si urobili test.";
        }
    }
}
