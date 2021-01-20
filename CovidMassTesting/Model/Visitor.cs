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
        /// Language in which we will communicate to the visitor
        /// 
        /// sk | en
        /// </summary>
        public string Language { get; set; } = "sk";
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
        public string Result { get; set; } = TestResult.NotTaken;
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet { get; set; }
        /// <summary>
        /// Last change
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }

        /// <summary>
        /// Real time when the test has been taken
        /// </summary>
        public DateTimeOffset? TestingTime { get; set; }
        /// <summary>
        /// Time, when visitor has clicked that he is in the queue
        /// </summary>
        public DateTimeOffset? Enqueued { get; set; }
        /// <summary>
        /// Product id
        /// </summary>
        public string Product { get; set; }
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId { get; set; }
        /// <summary>
        /// Captcha token. After it is used it is removed
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// If set to false, the icon on the map should be differnt with the icon where reservation is possible
        /// </summary>
        public bool HasReservationSystem { get; set; } = false;

        /// <summary>
        /// Link to external reservation system
        /// </summary>
        public string ExternalReservationSystem { get; set; }
    }
}
