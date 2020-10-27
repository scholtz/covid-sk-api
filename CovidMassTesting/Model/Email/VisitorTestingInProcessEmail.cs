using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Sends information to visitor that his test has been processed
    /// </summary>
    public class VisitorTestingInProcessEmail : IEmail
    {
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "TestingResult";
        /// <summary>
        /// Visitor name
        /// </summary>
        public string Name { get; set; }

    }
}
