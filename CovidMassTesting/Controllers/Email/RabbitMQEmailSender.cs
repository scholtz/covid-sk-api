using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using CovidMassTesting.Model.Email;
using CovidMassTesting.Model.SMS;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
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

                    var msg = new SendGridMessage()
                    {
                        TemplateId = data.TemplateId,
                        Personalizations = new List<Personalization>()
                        {
                            new Personalization()
                            {
                                TemplateData = data
                            }
                        }
                    };

                    if (!string.IsNullOrEmpty(subject))
                    {
                        msg.Subject = subject;
                    }

                    msg.AddTo(new EmailAddress(toEmail, toName));

                    msg.From = new EmailAddress(settings.Value.FromEmail, settings.Value.FromName);
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
                    var serialize = Encoding.UTF8.GetBytes(msg.Serialize());

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
                                         body: serialize);
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
