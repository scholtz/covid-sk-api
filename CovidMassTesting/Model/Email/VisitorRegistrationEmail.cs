using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Email
{
    public class VisitorRegistrationEmail : IEmail
    {
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "VisitorRegistration";

        public string Code { get; set; }
        public string BarCode { get; set; }
        public string Name { get; set; }
    }
}
