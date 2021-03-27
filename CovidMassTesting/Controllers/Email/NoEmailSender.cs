using CovidMassTesting.Model.Email;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.Email
{
    /// <summary>
    /// If no email sender is defined
    /// </summary>
    public class NoEmailSender : IEmailSender
    {
        /// <summary>
        /// For unit tests
        /// </summary>
        public ConcurrentDictionary<long, (string subject, string toEmail, string toName, IEmail data, IEnumerable<SendGrid.Helpers.Mail.Attachment> attachments)> Data { get; private set; } = new ConcurrentDictionary<long, (string, string, string, IEmail, IEnumerable<SendGrid.Helpers.Mail.Attachment>)>();
        /// <summary>
        /// Act as email was sent. Log event to console
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(string subject, string toEmail, string toName, IEmail data, IEnumerable<SendGrid.Helpers.Mail.Attachment> attachments)
        {
            System.Console.WriteLine($"Email: {subject} {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");
            await Task.Delay(1);
            Data[DateTimeOffset.Now.UtcTicks] = (subject, toEmail, toName, data, attachments);
            return true;
        }
    }
}
