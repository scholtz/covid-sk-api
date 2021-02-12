using Microsoft.Extensions.Logging;
using CovidMassTesting.Model.Email;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
        private readonly IOptions<Model.Settings.SendGridConfiguration> settings;
        /// <summary>
        /// Constructor
        /// </summary>
        public SendGridController(
            ILogger<SendGridController> logger,
            IOptions<Model.Settings.SendGridConfiguration> settings
            )
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            this.settings = settings;
            try
            {
                this.logger = logger;
                if (string.IsNullOrEmpty(settings.Value.MailerApiKey)) throw new Exception("Invalid SendGrid configuration");

                client = new SendGridClient(settings.Value.MailerApiKey);
                fromName = settings.Value.MailerFromName;
                fromEmail = settings.Value.MailerFromEmail;

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
        /// <param name="attachments"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(
            string subject,
            string toEmail,
            string toName,
            IEmail data,
            IEnumerable<Attachment> attachments
            )
        {
            try
            {
                if (data == null) throw new Exception("Please define data for email");
                if (string.IsNullOrEmpty(toEmail))
                {
                    logger.LogDebug($"Message {data.TemplateId} not delivered because email is not defined");
                    return false;
                }
                logger.LogInformation($"Sending {data.TemplateId} email to {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toEmail)}");
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
                if (!string.IsNullOrEmpty(settings.Value.ReplyToEmail))
                {
                    if (!string.IsNullOrEmpty(settings.Value.ReplyToName))
                    {
                        msg.ReplyTo = new EmailAddress(settings.Value.ReplyToEmail, settings.Value.ReplyToName);
                    }
                    else
                    {
                        msg.ReplyTo = new EmailAddress(settings.Value.ReplyToEmail);
                    }
                }
                if (attachments.Any())
                {
                    msg.AddAttachments(attachments);
                }
                var serialize = msg.Serialize();
                var response = await client.RequestAsync(SendGridClient.Method.POST, requestBody: serialize, urlPath: "mail/send");
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation($"Sent {data.TemplateId} email to {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toEmail)}");
                }
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted) return true;

                logger.LogError(await response.Body.ReadAsStringAsync());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error while sending email through sendgrid");
            }
            return false;
        }
    }

}
