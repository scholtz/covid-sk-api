using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using CovidMassTesting.Model.SMS;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.SMS
{
    /// <summary>
    /// https://doc.gosms.cz/#dokumentace-gosms-api
    /// </summary>
    public class GoSMSQueueSender : ISMSSender
    {
        private readonly AmazonSQSClient amazonSQSClient;
        private readonly ILogger<GoSMSQueueSender> logger;
        private readonly IOptions<Model.Settings.GoSMSQueueConfiguration> settings;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public GoSMSQueueSender(
            IStringLocalizer<GoSMSQueueSender> localizer,
            IOptions<Model.Settings.GoSMSQueueConfiguration> settings,
            ILogger<GoSMSQueueSender> logger
            )
        {
            if (localizer is null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.Value.QueueURL)) throw new Exception(localizer["Invalid SMS endpoint"].Value);
            var sqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Value.Region)
            };
            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(settings.Value.AccessKeyID, settings.Value.SecretAccessKey);
            amazonSQSClient = new AmazonSQSClient(awsCredentials, sqsConfig);
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

                    var msg = new GoSMSSendMessage()
                    {
                        channel = settings.Value.Channel,
                        message = text,
                        recipients = toPhone
                    };

                    var sendMessageRequest = new SendMessageRequest
                    {
                        QueueUrl = settings.Value.QueueURL,
                        MessageBody = JsonConvert.SerializeObject(msg)
                    };
                    logger.LogInformation($"Sending SMS {Helpers.Hash.GetSHA256Hash(settings.Value.CoHash + toPhone)}");
                    await amazonSQSClient.SendMessageAsync(sendMessageRequest);
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to PostMessagesAsync to sms queue. Exception: {ex.Message}");
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
