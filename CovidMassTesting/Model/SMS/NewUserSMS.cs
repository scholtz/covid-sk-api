using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// New system user SMS
    /// </summary>
    public class NewUserSMS : ISMS
    {
        /// <summary>
        /// User
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Generates message
        /// </summary>
        /// <returns></returns>
        public override string GetText()
        {
            return $"Dobrý deň {User}, zaregistrovali sme Vás do aplikácie hromadného testovania obyvateľov. Skontrolujte si prosím email.";
        }
    }
}
