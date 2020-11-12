using CovidMassTesting.Model.SMS;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.SMS
{
    /// <summary>
    /// Interface for email client
    /// </summary>
    public interface ISMSSender
    {
        /// <summary>
        /// Send email
        /// </summary>
        Task<bool> SendSMS(
            string toPhone,
            ISMS data
            );
    }
}
