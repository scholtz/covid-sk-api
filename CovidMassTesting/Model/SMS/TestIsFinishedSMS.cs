using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// Test has been finished, person should take the certificate.
    /// 
    /// We do not dispose medical information over SMS or Email. Person can check it online.
    /// </summary>
    public class TestIsFinishedSMS : ISMS
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
            return $"Dobrý deň {Name}. Váš test je dokončený. Dostavte sa na výdaj certifikátov.";
        }
    }
}
