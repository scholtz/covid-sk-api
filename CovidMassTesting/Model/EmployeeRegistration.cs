using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Registration obje
    /// </summary>
    public class EmployeeRegistration
    {
        public string EmployeeId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTimeOffset Time { get; set; }
        public string ProductId { get; set; }

    }
}
