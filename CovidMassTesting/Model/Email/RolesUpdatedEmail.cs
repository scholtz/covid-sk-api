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
        /// Template identifier
        /// </summary>
        public override string TemplateId => "Invitation";

        public string Name { get; set; }
        public string[] Roles { get; set; }
        public string Password { get; set; }
    }
}
