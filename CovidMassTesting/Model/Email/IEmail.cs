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
    }
}
