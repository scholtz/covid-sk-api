using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// GoSMS Queue support
    /// </summary>
    public class GoSMSQueueConfiguration
    {
        /// <summary>
        /// Region
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// SQS Endpoint
        /// </summary>
        public string QueueURL { get; set; }
        /// <summary>
        /// Client Secret
        /// </summary>
        public string SecretAccessKey { get; set; }
        /// <summary>
        /// Client identifier
        /// </summary>
        public string AccessKeyID { get; set; }
        /// <summary>
        /// Channel
        /// </summary>
        public int Channel { get; set; }
        /// <summary>
        /// CoHash
        /// </summary>
        public string CoHash { get; set; } = "";
    }
}
