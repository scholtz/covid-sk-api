namespace CovidMassTesting.Controllers.Email
{

    /// <summary>
    /// Sendgrid Email templating : https://sendgrid.com/docs/API_Reference/Web_API_v3/Transactional_Templates/templates.html
    /// </summary>
    public class SendgridTemplate
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
    }
    /// <summary>
    /// List of templates
    /// </summary>
    public class SendgridTemplates
    {
        /// <summary>
        /// templates
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public SendgridTemplate[] Templates { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
