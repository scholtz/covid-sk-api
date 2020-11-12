using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// We notify person by sms after we match testing set with visitor
    /// </summary>
    public class TestIsInProcessingSMS : ISMS
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
            return $"Dobrý deň {Name}, vašu vzorku sme odobrali a ideme ju spracovať. Technický test môže trvať 15-30 minút.";
        }
    }
}
