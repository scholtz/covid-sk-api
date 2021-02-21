using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.Settings
{
    /// <summary>
    /// Export task configuraion
    /// </summary>
    public class ExportTaskConfiguration
    {
        /// <summary>
        /// Send to list of emails
        /// </summary>
        public string[] Emails { get; set; }
    }
}
