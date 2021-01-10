using CovidMassTesting.Model.Email;
using System.Collections.Generic;
using System.Linq;
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
            string subject,
            string toEmail,
            string toName,
            IEmail data,
            IEnumerable<SendGrid.Helpers.Mail.Attachment> attachments
            );

        /// <summary>
        /// Email without attachment
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Task<bool> SendEmail(
            string subject,
            string toEmail,
            string toName,
            IEmail data
            )
        {
            return SendEmail(subject, toEmail, toName, data, Enumerable.Empty<SendGrid.Helpers.Mail.Attachment>());
        }

    }
}
