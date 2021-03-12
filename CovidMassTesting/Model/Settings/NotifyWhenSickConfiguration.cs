using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// Configuration for notification when someone is sick
    /// </summary>
    public class NotifyWhenSickConfiguration
    {
        /// <summary>
        /// Send to listed emails
        /// </summary>
        public List<Model.Email.EmailNameTuple> Emails { get; set; } = new List<Email.EmailNameTuple>();
    }
}
