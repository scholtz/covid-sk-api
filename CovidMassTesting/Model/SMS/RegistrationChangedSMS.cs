using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// When registration has been changed, send sms message
    /// </summary>
    public class RegistrationChangedSMS : ISMS
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
            return $"Vašu registráciu s kódom {Code} sme upravili. {Name}, termín: {Date}, odberné miesto: {Place}";
        }
    }
}