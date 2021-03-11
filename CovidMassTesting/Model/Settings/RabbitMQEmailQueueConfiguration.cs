using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    public class RabbitMQEmailQueueConfiguration
    {
        /// <summary>
        /// Hostname
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// User
        /// </summary>
        public string RabbitUserName { get; set; }
        /// <summary>
        /// Password
        /// </summary>
        public string RabbitPassword { get; set; }
        /// <summary>
        /// Virtual host
        /// </summary>
        public string VirtualHost { get; set; }
        /// <summary>
        /// Queue name
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// Exchange
        /// </summary>
        public string Exchange { get; set; }
        /// <summary>
        /// Cohash for loging purposes
        /// </summary>
        public string CoHash { get; set; }
        public string FromEmail { get; internal set; }
        public string FromName { get; internal set; }
        public string ReplyToEmail { get; internal set; }
        public string ReplyToName { get; internal set; }
    }
}
