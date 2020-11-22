namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Email sent when test was not positive not negative, and needs to be repeated
    /// </summary>
    public class VisitorTestingToBeRepeatedEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language"></param>
        public VisitorTestingToBeRepeatedEmail(string language)
        {
            SetLanguage(language);
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "TestingToRepeat";
        /// <summary>
        /// Visitor name
        /// </summary>
        public string Name { get; set; }
    }
}
