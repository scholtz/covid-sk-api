using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// GoSMS configuration https://doc.gosms.cz/
    /// </summary>
    public class GoSMSConfiguration
    {
        public string Endpoint { get; set; } = "https://app.gosms.cz/";
        /// <summary>
        /// Client identifier
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Client Secret
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// Channel
        /// </summary>
        public int Channel { get; set; }

    }
}
