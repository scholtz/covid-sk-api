using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// Mailgun configuration
    /// </summary>
    public class MailGunConfiguration
    {
        /// <summary>
        /// Endpoint
        /// </summary>
        public string Endpoint { get; set; } = "https://api.eu.mailgun.net/v3";
        /// <summary>
        /// Sendgrid domain
        /// </summary>
        public string Domain { get; set; }
        /// <summary>
        /// Api key
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// From name
        /// </summary>
        public string MailerFromName { get; set; }
        /// <summary>
        /// From email
        /// </summary>
        public string MailerFromEmail { get; set; }
    }
}
