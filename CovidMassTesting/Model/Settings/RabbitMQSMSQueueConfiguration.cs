using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    public class RabbitMQSMSQueueConfiguration
    {
        public string HostName { get; set; }
        public string RabbitUserName { get; set; }
        public string RabbitPassword { get; set; }
        public string VirtualHost { get; set; }
        public string QueueName { get; set; }
        public string Exchange { get; set; }
        public string GatewayUser { get; set; }
    }
}
