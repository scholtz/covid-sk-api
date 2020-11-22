using CovidMassTesting.Model.SMS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.SMS
{
    /// <summary>
    /// https://doc.gosms.cz/#dokumentace-gosms-api
    /// </summary>
    public class GoSMSSender : ISMSSender
    {
        private readonly IStringLocalizer<GoSMSSender> localizer;
        private readonly ILogger<GoSMSSender> logger;
        private readonly IOptions<Model.Settings.GoSMSConfiguration> settings;
        private readonly RestClient smsApiRestClient;
        private DateTimeOffset tokenExpire = DateTimeOffset.MinValue;
        private string token;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        public GoSMSSender(
            IStringLocalizer<GoSMSSender> localizer,
            IOptions<Model.Settings.GoSMSConfiguration> settings,
            ILogger<GoSMSSender> logger
            )
        {
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.Value.Endpoint)) throw new Exception(localizer["Invalid SMS endpoint"].Value);
            smsApiRestClient = new RestClient(settings.Value.Endpoint);
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
                if (string.IsNullOrEmpty(toPhone))
                {
                    throw new ArgumentException(localizer["Phone must not be empty"].Value);
                }

                if (data is null)
                {
                    throw new ArgumentNullException(localizer["SMS data must not be empty"].Value);
                }

                var token = await GetToken();
                logger.LogInformation($"Sending SMS message to {toPhone}");
                var request = new RestRequest("api/v1/messages", Method.POST, DataFormat.Json);
                request.AddJsonBody(new GoSMSSendMessage()
                {
                    channel = settings.Value.Channel,
                    message = data.GetText(),
                    recipients = toPhone
                });
                request.AddHeader("Authorization", $"Bearer {token}");
                var response = await smsApiRestClient.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                logger.LogError($"Error sending sms: {response.Content}");
                return false;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, $"Error sending SMS to: {toPhone} {exc.Message}");
                return false;
            }
        }
        private async Task<string> GetToken()
        {
            if (tokenExpire > DateTimeOffset.Now)
            {
                return token;
            }

            var request = new RestRequest("oauth/v2/token", Method.POST, DataFormat.Json);
            request.AddParameter("client_id", settings.Value.ClientId);
            request.AddParameter("client_secret", settings.Value.ClientSecret);
            request.AddParameter("grant_type", "client_credentials");

            var response = await smsApiRestClient.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<Model.SMS.GoSMSAuthMessage>(response.Content);
                if (string.IsNullOrEmpty(data?.token_type))
                {
                    throw new Exception("Invalid access token");
                }

                if (data.expires_in > 0)
                {
                    this.tokenExpire = DateTimeOffset.Now.AddSeconds(data.expires_in).AddSeconds(-10);
                }
                this.token = data.access_token;
                return token;
            }

            throw new Exception(string.Format(localizer["Unable to parse access token: {0}"], response.Content));
        }
    }
}
