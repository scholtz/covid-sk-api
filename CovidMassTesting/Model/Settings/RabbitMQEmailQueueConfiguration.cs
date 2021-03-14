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
        /// <summary>
        /// Send emails from email 
        /// </summary>
        public string FromEmail { get; set; }
        /// <summary>
        /// Name of the person who seems to send the emails
        /// </summary>
        public string FromName { get; set; }
        /// <summary>
        /// When person clicks on reply to, the email
        /// </summary>
        public string ReplyToEmail { get; set; }
        /// <summary>
        /// When person clicks on reply to, the name who will reply to
        /// </summary>
        public string ReplyToName { get; set; }
    }
}
