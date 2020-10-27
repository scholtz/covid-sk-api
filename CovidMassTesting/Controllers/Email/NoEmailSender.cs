using CovidMassTesting.Model.Email;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.Email
{
    /// <summary>
    /// If no email sender is defined
    /// </summary>
    public class NoEmailSender : IEmailSender
    {
        /// <summary>
        /// Act as email was sent
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(string toEmail, string toName, IEmail data)
        {
            await Task.Delay(1);
            return true;
        }
    }
}
