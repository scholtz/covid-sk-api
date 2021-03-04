using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.SMS
{
    public class RabbitSMSMessage
    {
        public string Phone { get; set; }
        public string Message { get; set; }
        public string User { get; set; }
    }
}