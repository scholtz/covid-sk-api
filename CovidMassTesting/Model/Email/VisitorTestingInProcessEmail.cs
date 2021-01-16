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
        /// Constructor
        /// </summary>
        /// <param name="language"></param>
        public VisitorTestingInProcessEmail(string language)
        {
            SetLanguage(language);
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "TestingInProcess";
        /// <summary>
        /// Visitor name
        /// </summary>
        public string Name { get; set; }

    }
}
