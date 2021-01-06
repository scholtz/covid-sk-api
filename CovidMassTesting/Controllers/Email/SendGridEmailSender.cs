using Microsoft.Extensions.Logging;
using CovidMassTesting.Model.Email;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CovidMassTesting.Controllers.Email
{
    /// <summary>
    /// Sendgrid email sender
    /// </summary>
    public class SendGridController : IEmailSender
    {
        private readonly SendGridClient client;
        private readonly string fromName;
        private readonly string fromEmail;
        private readonly Dictionary<string, string> Name2Id;
        private readonly ILogger<SendGridController> logger;
        /// <summary>
        /// Constructor
        /// </summary>
        public SendGridController(
            ILogger<SendGridController> logger,
            IConfiguration configuration
            )
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            try
            {
                this.logger = logger;
                var config = new Model.Settings.SendGridConfiguration();
                configuration.GetSection("SendGrid").Bind(config);
                if (string.IsNullOrEmpty(config.MailerApiKey)) throw new Exception("Invalid SendGrid configuration");

                client = new SendGridClient(config.MailerApiKey);
                fromName = config.MailerFromName;
                fromEmail = config.MailerFromEmail;

                var response = client.RequestAsync(
                    SendGridClient.Method.GET,
                    urlPath: "/templates",
                    queryParams: "{\"generations\": \"dynamic\"}").Result.Body.ReadAsStringAsync().Result;

                var list = Newtonsoft.Json.JsonConvert.DeserializeObject<SendgridTemplates>(response);
                if (list.Templates.Length == 0) throw new Exception("Email templates are not set up in sendgrid");
                Name2Id = list.Templates.ToDictionary(k => k.Name, k => k.Id);
                logger.LogInformation($"SendGridController configured {list.Templates.Length}");
            }
            catch (Exception exc)
            {
                logger.LogError(exc.Message, exc);
            }
        }
        /// <summary>
        /// Semd email
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(
            string subject,
            string toEmail,
            string toName,
            IEmail data
            )
        {
            if (data == null) throw new Exception("Please define data for email");
            if (string.IsNullOrEmpty(toEmail))
            {
                logger.LogDebug($"Message {data.TemplateId} not delivered because email is not defined");
                return false;
            }
            logger.LogInformation($"Sending {data.TemplateId} email to {toEmail}");
            if (!Name2Id.ContainsKey(data.TemplateId))
            {
                System.Console.WriteLine($"Template not found: {data.TemplateId}: {subject} {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");
                return false;
            }
            var msg = new SendGridMessage()
            {

                TemplateId = Name2Id[data.TemplateId],
                Personalizations = new List<Personalization>()
                {
                    new Personalization()
                    {
                        TemplateData = data
                    }
                }
            };
            msg.AddTo(new EmailAddress(toEmail, toName));

            msg.From = new EmailAddress(fromEmail, fromName);

            var serialize = msg.Serialize();
            var response = await client.RequestAsync(SendGridClient.Method.POST, requestBody: serialize, urlPath: "mail/send");
            if (response.StatusCode == System.Net.HttpStatusCode.Accepted) return true;

            logger.LogError(await response.Body.ReadAsStringAsync());
            logger.LogError(serialize);
            return false;
        }
    }

}
