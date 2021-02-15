using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// User with permissions to invite other people can invite them and 
    /// </summary>
    public class InvitationEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public InvitationEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            this.Website = url;
            this.SupportEmail = supportEmail;
            this.SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "InvitationEmail";
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Roles
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Roles { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Name of person who inveted user
        /// </summary>
        public string InviterName { get; set; }
        /// <summary>
        /// Company name
        /// </summary>
        public string CompanyName { get; set; }
        /// <summary>
        /// Company name
        /// </summary>
        public string WebPath { get; set; }
    }
}
