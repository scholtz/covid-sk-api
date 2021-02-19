namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Sends information to visitor that his test has been processed
    /// </summary>
    public class VisitorTestingInProcessEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public VisitorTestingInProcessEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            Website = url;
            SupportEmail = supportEmail;
            SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "TestingInProcess";
        /// <summary>
        /// Visitor name
        /// </summary>
        public string Name { get; set; }

    }
}
