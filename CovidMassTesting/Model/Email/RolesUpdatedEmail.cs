using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Email
{
    /// <summary>
    /// User with permissions to invite other people can invite them and 
    /// </summary>
    public class RolesUpdatedEmail : IEmail
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="language"></param>
        public RolesUpdatedEmail(string language)
        {
            SetLanguage(language);
        }
        /// <summary>
        /// Template identifier
        /// </summary>
        public override string TemplateId => "Invitation";
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Roles
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public string[] Roles { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }
    }
}
