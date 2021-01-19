using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// Sends information to visitor that his test has been processed
    /// </summary>
    public class VisitorTestingResultEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language"></param>
        public VisitorTestingResultEmail(string language)
        {
            SetLanguage(language);
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "TestingResult";
        /// <summary>
        /// Visitor name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Is sick
        /// </summary>
        public bool IsSick { get; set; }
    }
}
