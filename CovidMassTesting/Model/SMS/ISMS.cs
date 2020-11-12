namespace CovidMassTesting.Model.SMS
{
    /// <summary>
    /// SMS abstraction class
    /// </summary>
    public abstract class ISMS
    {
        /// <summary>
        /// Returns text of sms message
        /// </summary>
        public abstract string GetText();
    }
}
