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
        /// Act as email was sent. Log event to console
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(string subject, string toEmail, string toName, IEmail data)
        {
            System.Console.WriteLine($"Email: {subject} {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");
            await Task.Delay(1);
            return true;
        }
    }
}
