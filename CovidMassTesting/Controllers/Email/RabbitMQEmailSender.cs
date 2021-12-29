using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using CovidMassTesting.Model.Email;
using CovidMassTesting.Model.SMS;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.Email
{
    /// <summary>
    /// https://doc.gosms.cz/#dokumentace-gosms-api
    /// </summary>
    public class RabbitMQEmailSender : IEmailSender
    {
        private readonly ILogger<RabbitMQEmailSender> logger;
        private readonly IOptions<Model.Settings.RabbitMQEmailQueueConfiguration> settings;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public RabbitMQEmailSender(
            IStringLocalizer<RabbitMQEmailSender> localizer,
            IOptions<Model.Settings.RabbitMQEmailQueueConfiguration> settings,
            ILogger<RabbitMQEmailSender> logger
            )
        {
            if (localizer is null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }
            this.settings = settings;
            this.logger = logger;
        }



        /// <summary>
        /// Rabbit MQ Email sender
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        /// <param name="toName"></param>
        /// <param name="data"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        public async Task<bool> SendEmail(string subject, string toEmail, string toName, IEmail data, IEnumerable<Attachment> attachments)
        {
            try
            {
                try
                {
                    logger.LogInformation($"Sending {data.TemplateId} email to {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toEmail)}");
                    var mailMessage = new MimeMessage();
                    mailMessage.From.Add(new MailboxAddress(settings.Value.FromName, settings.Value.FromEmail));
                    mailMessage.To.Add(new MailboxAddress(toName, toEmail));
                    mailMessage.ReplyTo.Add(new MailboxAddress(settings.Value.ReplyToName, settings.Value.ReplyToEmail));
                    mailMessage.Subject = subject;
                    var bodyBuilder = new BodyBuilder();
                    bodyBuilder.TextBody = data.TemplateId;
                    bodyBuilder.HtmlBody = JsonConvert.SerializeObject(data);

                    if (attachments.Any())
                    {
                        foreach (var attachment in attachments)
                        {
                            bodyBuilder.Attachments.Add(attachment.Filename, Convert.FromBase64String(attachment.Content));
                        }
                    }


                    mailMessage.Body = bodyBuilder.ToMessageBody();
                    using var memoryStream = new MemoryStream();
                    mailMessage.WriteTo(memoryStream);
                    ;
                    var factory = new ConnectionFactory()
                    {
                        HostName = settings.Value.HostName,
                        UserName = settings.Value.RabbitUserName,
                        Password = settings.Value.RabbitPassword,
                        VirtualHost = settings.Value.VirtualHost
                    };
                    using var connection = factory.CreateConnection();
                    using var channel = connection.CreateModel();
                    channel.BasicPublish(exchange: settings.Value.Exchange,
                                         routingKey: settings.Value.QueueName,
                                         body: memoryStream.ToArray());

                    logger.LogInformation($"Sent {data.TemplateId} email to {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toEmail)}");
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to PostMessagesAsync to rabbit sms queue. Exception: {ex.Message}");
                    throw;
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, $"Failed to PostMessagesAsync to rabbit sms queue. Exception: {exc.Message}");
                return false;
            }
        }
    }
}
