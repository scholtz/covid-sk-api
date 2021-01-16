using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class VisitorSimplified
    {
        /// <summary>
        /// Registration code. 9-digit, formatted 000-000-000 for visitors
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Real time when the test has been taken
        /// </summary>
        public DateTimeOffset? TestingTime { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// Language in which we will communicate to the visitor
        /// 
        /// sk | en
        /// </summary>
        public string Language { get; set; } = "sk";
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet { get; set; }
        /// <summary>
        /// Product id
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId { get; set; }
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
    }
}
