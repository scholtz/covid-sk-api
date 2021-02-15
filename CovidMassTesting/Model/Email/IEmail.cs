namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Email abstraction class
    /// </summary>
    public abstract class IEmail
    {
        /// <summary>
        /// Identifier of the template
        /// </summary>
        public abstract string TemplateId { get; }
        /// <summary>
        /// Support email
        /// </summary>
        public string SupportEmail { get; set; }
        /// <summary>
        /// Support phone
        /// </summary>
        public string SupportPhone { get; set; }
        /// <summary>
        /// Website
        /// </summary>
        public string Website { get; set; }
        /// <summary>
        /// Is English
        /// </summary>
        public bool IsEN { get; set; }
        /// <summary>
        /// Is Slovak language
        /// </summary>
        public bool IsSK { get; set; }
        /// <summary>
        /// Is Czech language
        /// </summary>
        public bool IsCS { get; set; }
        /// <summary>
        /// Is German language
        /// </summary>
        public bool IsDE { get; set; }
        /// <summary>
        /// Is hungarian language
        /// </summary>
        public bool IsHU { get; set; }
        /// <summary>
        /// Set the language
        /// </summary>
        /// <param name="language"></param>
        protected void SetLanguage(string language)
        {
            switch (language)
            {
                case "sk":
                case "sk-SK":
                    this.IsSK = true;
                    break;

                case "cs":
                case "cs-CZ":
                    this.IsCS = true;
                    break;

                case "hu":
                case "hu-HU":
                    this.IsHU = true;
                    break;

                case "de":
                case "de-AT":
                case "de-DE":
                    this.IsDE = true;
                    break;
                default:
                    this.IsEN = true;
                    break;
            }
        }
    }
}
