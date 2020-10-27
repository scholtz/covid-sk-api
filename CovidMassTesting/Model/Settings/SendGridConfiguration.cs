namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// configuration for mailing through sendgrid
    /// </summary>
    public class SendGridConfiguration
    {
        /// <summary>
        /// API key
        /// </summary>
        public string MailerApiKey { get; set; }
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
