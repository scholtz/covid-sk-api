using CovidMassTesting.Model.EZdravie.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie
{
    public class BaseResponse
    {
        public int ErrorCount { get; set; }
        public InfoPayload[] Errors { get; set; }
        public InfoPayload[] Info { get; set; }
        public int PayloadCount { get; set; }
        public int WarningCount { get; set; }
        public InfoPayload[] Warnings { get; set; }
    }
}
