using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// One person can be employed in multiple companies
    /// </summary>
    public class CompanyIdentifier
    {
        /// <summary>
        /// Company name
        /// </summary>
        public string CompanyName { get; set; }
        /// <summary>
        /// Company identifier
        /// </summary>
        public string CompanyId { get; set; }
        /// <summary>
        /// Employee identifier in the company
        /// </summary>
        public string EmployeeId { get; set; }
    }
}
