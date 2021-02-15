namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Email sent when test was not positive not negative, and needs to be repeated
    /// </summary>
    public class PersonalDataRemovedEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public PersonalDataRemovedEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            this.Website = url;
            this.SupportEmail = supportEmail;
            this.SupportPhone = SupportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "PersonalDataRemoved";
        /// <summary>
        /// Visitor name
        /// </summary>
        public string Name { get; set; }
    }
}
