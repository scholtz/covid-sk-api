using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Email template for visitor change registration
    /// </summary>
    public class VisitorChangeRegistrationEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public VisitorChangeRegistrationEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            this.Website = url;
            this.SupportEmail = supportEmail;
            this.SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "VisitorChangeRegistration";
        /// <summary>
        /// Registration code
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// BarCode image
        /// </summary>
        public string BarCode { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Date
        /// </summary>
        public string Date { get; set; }
        /// <summary>
        /// Place
        /// </summary>
        public string Place { get; set; }
        /// <summary>
        /// Place description
        /// </summary>
        public string PlaceDescription { get; set; }
    }
}
