namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// User with permissions to invite other people can invite them and 
    /// </summary>
    public class GenericEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language">Current language</param>
        /// <param name="url">Web of the system</param>
        /// <param name="supportEmail">Email Support</param>
        /// <param name="supportPhone">Phone Support</param>
        public GenericEmail(string language, string url, string supportEmail, string supportPhone)
        {
            SetLanguage(language);
            Website = url;
            SupportEmail = supportEmail;
            SupportPhone = supportPhone;
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "GenericEmail";
        /// <summary>
        /// Text in Slovak language
        /// </summary>
        public string TextSK { get; set; }
        /// <summary>
        /// Text in Czech language
        /// </summary>
        public string TextCS { get; set; }
        /// <summary>
        /// Text in English language
        /// </summary>
        public string TextEN { get; set; }
    }
}
