using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using CovidMassTesting.Model.SMS;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.SMS
{
    /// <summary>
    /// https://doc.gosms.cz/#dokumentace-gosms-api
    /// </summary>
    public class RabbitMQSMSSender : ISMSSender
    {
        private readonly ILogger<GoSMSQueueSender> logger;
        private readonly IOptions<Model.Settings.RabbitMQSMSQueueConfiguration> settings;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public RabbitMQSMSSender(
            IStringLocalizer<GoSMSQueueSender> localizer,
            IOptions<Model.Settings.RabbitMQSMSQueueConfiguration> settings,
            ILogger<GoSMSQueueSender> logger
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
        /// Act as sms was sent. Log event to console
        /// </summary>
        /// <param name="toPhone"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> SendSMS(string toPhone, ISMS data)
        {
            try
            {
                try
                {
                    var text = data.GetText();
                    if (text.Length >= 70)
                    {
                        text = Helpers.Text.RemoveDiacritism(text);
                    }

                    if (text.Length > 160)
                    {
                        text = text.Substring(0, 158) + "..";
                    }

                    var msg = new RabbitSMSMessage()
                    {
                        Phone = toPhone,
                        Message = text,
                        User = settings.Value.GatewayUser
                    };
                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));


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
                                         body: body);
                    logger.LogInformation($"Sent SMS to {settings.Value.HostName}/{settings.Value.VirtualHost}/{settings.Value.QueueName} {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toPhone)}");

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
                logger.LogError(exc, $"Error sending SMS to: {toPhone} {exc.Message}");
                return false;
            }
        }
    }
}
