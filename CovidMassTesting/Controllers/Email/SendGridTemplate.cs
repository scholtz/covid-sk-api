using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers.Email
{

    /// <summary>
    /// Sendgrid Email templating : https://sendgrid.com/docs/API_Reference/Web_API_v3/Transactional_Templates/templates.html
    /// </summary>
    public class SendgridTemplate
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
    }
    /// <summary>
    /// List of templates
    /// </summary>
    public class SendgridTemplates
    {
        /// <summary>
        /// templates
        /// </summary>
        public SendgridTemplate[] Templates { get; set; }
    }
}
