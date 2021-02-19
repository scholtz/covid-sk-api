namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// GoSMS configuration https://doc.gosms.cz/
    /// </summary>
    public class GoSMSConfiguration
    {
        /// <summary>
        /// Endpoing
        /// </summary>
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
        /// <summary>
        /// Logging info
        /// </summary>
        public int CoHash { get; set; }

    }
}
