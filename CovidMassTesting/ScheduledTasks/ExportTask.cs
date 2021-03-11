using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Repository.Interface;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.ScheduledTasks
{
    /// <summary>
    /// Export task
    /// </summary>
    public class ExportTask : IScheduledTask
    {
        private readonly ILogger<ExportTask> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly IConfiguration configuration;
        private readonly IOptions<Model.Settings.ExportTaskConfiguration> settings;
        private readonly IEmailSender emailSender;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="emailSender"></param>
        public ExportTask(
            ILogger<ExportTask> logger,
            IConfiguration configuration,
            IOptions<Model.Settings.ExportTaskConfiguration> options,
            IVisitorRepository visitorRepository,
            IEmailSender emailSender
            )
        {
            this.logger = logger;
            this.configuration = configuration;
            this.settings = options;
            this.visitorRepository = visitorRepository;
            this.emailSender = emailSender;
        }
        /// <summary>
        /// Process
        /// </summary>
        /// <returns></returns>
        public Task<bool> Process()
        {
            return Process(DateTimeOffset.Now.AddDays(-1));
        }
        /// <summary>
        /// Export data and send it to configured email
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Process(DateTimeOffset dayTimeStamp)
        {
            logger.LogInformation("Processing export");
            if (settings.Value.Emails == null || settings.Value.Emails.Length == 0)
            {
                logger.LogInformation("No emails defined");
                return false;
            }
            logger.LogInformation($"Will send to: {string.Join(";", settings.Value.Emails)}");
            var attachments = new List<SendGrid.Helpers.Mail.Attachment>();

            var days = await visitorRepository.ListExportableDays();
            var day = days.FirstOrDefault(d => d.UtcDateTime >= dayTimeStamp.AddDays(-1) && d.UtcDateTime < dayTimeStamp);
            if (day == default(DateTimeOffset)) return false;
            var data = await visitorRepository.ListAnonymizedVisitors(day, 0, 9999999);
            logger.LogInformation($"Got {data.Count()} items for day {day.ToString("O")}");
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
            writer.Flush();
            var ret = stream.ToArray();
            attachments.Add(new SendGrid.Helpers.Mail.Attachment()
            {
                Content = Convert.ToBase64String(ret),
                Filename = $"{dayTimeStamp.ToString("yyyy-MM-dd")}.csv",
                Type = "text/csv",
                Disposition = "attachment"
            });
            foreach (var email in settings.Value.Emails)
            {
                await emailSender.SendEmail(
                    $"Report {dayTimeStamp.ToString("yyyy-MM-dd")}",
                    email,
                    "",
                    new Model.Email.GenericEmail("sk-SK", configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                    {
                        TextSK = $"<h1>Denný anonymizovaný report</h1><p>{configuration["EmailSupport"]}</p>"
                    },
                    attachments
                );
            }

            return true;
        }
    }
}
