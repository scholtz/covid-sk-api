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
    public class DeleteOldVisitors : IScheduledTask
    {
        private readonly ILogger<ExportTask> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly IConfiguration configuration;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="visitorRepository"></param>
        public DeleteOldVisitors(
            ILogger<ExportTask> logger,
            IConfiguration configuration,
            IVisitorRepository visitorRepository
            )
        {
            this.logger = logger;
            this.configuration = configuration;
            this.visitorRepository = visitorRepository;
        }
        /// <summary>
        /// Process
        /// </summary>
        /// <returns></returns>
        public Task<bool> Process()
        {
            return Process(14);
        }
        /// <summary>
        /// Export data and send it to configured email
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Process(int daysToKeep)
        {
            logger.LogInformation($"Scheduler: DeleteOldVisitors init {daysToKeep}");
            var ret = await visitorRepository.DeleteOldVisitors(daysToKeep);
            logger.LogInformation($"Scheduler: DeleteOldVisitors done {ret}");

            return true;
        }
    }
}
