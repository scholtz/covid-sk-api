using CovidMassTesting.Model.Email;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.Email
{
    /// <summary>
    /// Interface for email client
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Send email
        /// </summary>
        Task<bool> SendEmail(
            string toEmail,
            string toName,
            IEmail data
            );
    }
}
