using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Data stored in this object are encrypted
    /// 
    /// Stores personal data, contact data as well as medical condition
    /// </summary>
    public class Visitor
    {
        /// <summary>
        /// Registration code. 9-digit, formatted 000-000-000 for visitors
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Type of person
        /// 
        /// idcard|child|foreign
        /// </summary>
        public string PersonType { get; set; }
        /// <summary>
        /// Passport number if person type is foreigner
        /// </summary>
        public string Passport { get; set; }
        /// <summary>
        /// Personal number if person type is idcard or child
        /// </summary>
        public string RC { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// Address
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Phone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Insurance
        /// </summary>
        public string Insurance { get; set; }
        /// <summary>
        /// Chosen time slot
        /// </summary>
        public long ChosenSlot { get; set; }
        /// <summary>
        /// Chosen place
        /// </summary>
        public string ChosenPlaceId { get; set; }
        /// <summary>
        /// Test result. Available options are in Model.TestResult
        /// </summary>
        public string Result { get; set; } = "test-not-taken";
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet { get; set; }
        /// <summary>
        /// Last change
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }
    }
}
