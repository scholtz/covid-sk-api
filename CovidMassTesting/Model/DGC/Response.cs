using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.DGC
{
    public class Response
    {
        public string ErrorMessage { get; set; }
        public string Uvci { get; set; }
        public Attachment[] Attachments { get; set; }
    }
}
