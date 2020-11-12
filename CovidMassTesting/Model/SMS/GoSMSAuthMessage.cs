using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// https://doc.gosms.cz/#ziskani-access-tokenu
    /// </summary>
    public class GoSMSAuthMessage
    {
        /// <summary>
        /// Token
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public string access_token { get; set; }
#pragma warning restore CA1707 // Identifiers should not contain underscores
        /// <summary>
        /// Expire in seconds: 3600
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public int expires_in { get; set; }
#pragma warning restore CA1707 // Identifiers should not contain underscores
        /// <summary>
        /// token type: bearer
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public string token_type { get; set; }
#pragma warning restore CA1707 // Identifiers should not contain underscores
        /// <summary>
        /// scope: user
        /// </summary>
        public string scope { get; set; }
    }
}
