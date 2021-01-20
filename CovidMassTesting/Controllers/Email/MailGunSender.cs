using Microsoft.Extensions.Logging;
using CovidMassTesting.Model.Email;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Authenticators;

namespace CovidMassTesting.Controllers.Email
{
    /// <summary>
    /// Sendgrid email sender
    /// </summary>
    public class MailGunSender : IEmailSender
    {
        private readonly ILogger<MailGunSender> logger;
        private readonly Model.Settings.MailGunConfiguration config;
        /// <summary>
        /// Constructor
        /// </summary>
        public MailGunSender(
            ILogger<MailGunSender> logger,
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
                config = new Model.Settings.MailGunConfiguration();
                configuration.GetSection("MailGun").Bind(config);

                if (string.IsNullOrEmpty(config.ApiKey)) throw new Exception("Invalid MailGun configuration");

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
                logger.LogInformation($"Sending {data.TemplateId} email to {toEmail}");



                RestClient client = new RestClient();
                client.BaseUrl = new Uri(config.Endpoint);
                client.Authenticator = new HttpBasicAuthenticator("api", config.ApiKey);

                RestRequest request = new RestRequest();
                request.AddParameter("domain", config.Domain, ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", $"{config.MailerFromName} <{config.MailerFromEmail}>");
                request.AddParameter("to", $"{toName} <{toEmail}>");
                request.AddParameter("subject", subject);
                request.AddParameter("template", data.TemplateId);

                request.AddParameter("v:IsCS", data.IsCS);
                request.AddParameter("v:IsSK", data.IsSK);
                request.AddParameter("v:IsDE", data.IsDE);
                request.AddParameter("v:IsEN", data.IsEN);
                request.AddParameter("v:IsHU", data.IsHU);

                if (data is InvitationEmail)
                {
                    var emailData = data as InvitationEmail;
                    request.AddParameter("v:Password", emailData.Password);
                    request.AddParameter("v:CompanyName", emailData.CompanyName);
                    request.AddParameter("v:InviterName", emailData.InviterName);
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Roles", emailData.Roles);
                    request.AddParameter("v:WebPath", emailData.WebPath);
                }

                if (data is PersonalDataRemovedEmail)
                {
                    var emailData = data as PersonalDataRemovedEmail;
                    request.AddParameter("v:Name", emailData.Name);
                }

                if (data is RolesUpdatedEmail)
                {
                    var emailData = data as RolesUpdatedEmail;
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Password", emailData.Password);
                    request.AddParameter("v:Roles", emailData.Roles);
                }
                if (data is VisitorChangeRegistrationEmail)
                {
                    var emailData = data as VisitorChangeRegistrationEmail;
                    request.AddParameter("v:BarCode", emailData.BarCode);
                    request.AddParameter("v:Code", emailData.Code);
                    request.AddParameter("v:Date", emailData.Date);
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Place", emailData.Place);
                    request.AddParameter("v:PlaceDescription", emailData.PlaceDescription);
                }
                if (data is VisitorRegistrationEmail)
                {
                    var emailData = data as VisitorRegistrationEmail;
                    request.AddParameter("v:BarCode", emailData.BarCode);
                    request.AddParameter("v:Code", emailData.Code);
                    request.AddParameter("v:Date", emailData.Date);
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:Place", emailData.Place);
                    request.AddParameter("v:PlaceDescription", emailData.PlaceDescription);
                }
                if (data is VisitorTestingInProcessEmail)
                {
                    var emailData = data as VisitorTestingInProcessEmail;
                    request.AddParameter("v:Name", emailData.Name);
                }
                if (data is VisitorTestingResultEmail)
                {
                    var emailData = data as VisitorTestingResultEmail;
                    request.AddParameter("v:Name", emailData.Name);
                    request.AddParameter("v:IsSick", emailData.IsSick);
                }
                if (data is VisitorTestingToBeRepeatedEmail)
                {
                    var emailData = data as VisitorTestingToBeRepeatedEmail;
                    request.AddParameter("v:Name", emailData.Name);
                }

                foreach (var attachment in attachments)
                {
                    request.AddFile("attachment", Convert.FromBase64String(attachment.Content), attachment.Filename, attachment.Type);
                }
                request.Method = Method.POST;
                var response = client.Execute(request);
                return response.IsSuccessful;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error while sending email through mailgun");
                return false;
            }
        }
    }

}
