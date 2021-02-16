using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Helpers;

namespace CovidMassTesting.Model
{
    public class VisitorAnonymized
    {
        private readonly Visitor visitor;
        private readonly string cohash = "";
        public VisitorAnonymized(Visitor visitor, string cohash)
        {
            this.visitor = visitor;
            this.cohash = cohash;
        }
        /// <summary>
        /// Registration code. 9-digit, formatted 000-000-000 for visitors
        /// </summary>
        public string Id { get { return $"{cohash}{visitor.Id}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Language in which we will communicate to the visitor
        /// 
        /// sk | en
        /// </summary>
        public string Language { get { return visitor.Language; } }
        /// <summary>
        /// Type of person
        /// 
        /// idcard|child|foreign
        /// </summary>
        public string PersonType { get { return visitor.PersonType; } }
        /// <summary>
        /// Passport number if person type is foreigner
        /// </summary>
        public string Passport { get { return $"{cohash}{visitor.Passport}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Personal number if person type is idcard or child
        /// </summary>
        public string RC { get { return $"{cohash}{visitor.RC}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Employee id if applicable
        /// </summary>
        public string EmployeeId { get { return $"{cohash}{visitor.EmployeeId}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// BirthDay - day
        /// </summary>
        public int? BirthDayDay { get { return null; } }
        /// <summary>
        /// BirthDay - month
        /// </summary>
        public int? BirthDayMonth { get { return visitor.BirthDayMonth; } }
        /// <summary>
        /// BirthDay - year
        /// </summary>
        public int? BirthDayYear { get { return visitor.BirthDayYear; } }
        /// <summary>
        /// Name
        /// </summary>
        public string FirstName { get { return $"{cohash}{visitor.FirstName}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get { return $"{cohash}{visitor.LastName}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// ZIP - Pernament address
        /// </summary>
        public string ZIP { get { return visitor.ZIP; } }
        /// <summary>
        /// City - Pernament address
        /// </summary>
        public string City { get { return visitor.City; } }
        /// <summary>
        /// Street - Pernament address
        /// </summary>
        public string Street { get { return $"{cohash}{visitor.Street}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// StreetNo - Pernament address
        /// </summary>
        public string StreetNo { get { return $"{cohash}{visitor.StreetNo}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Address - Pernament address
        /// </summary>
        public string Address { get { return $"{cohash}{visitor.Address}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get { return $"{cohash}{visitor.Email}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Phone
        /// </summary>
        public string Phone { get { return $"{cohash}{visitor.Phone}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Insurance
        /// </summary>
        public string Insurance { get { return visitor.Insurance; } }
        /// <summary>
        /// Chosen time slot
        /// </summary>
        public long ChosenSlot { get { return visitor.ChosenSlot; } }
        /// <summary>
        /// ChosenSlotTime
        /// </summary>
        public DateTimeOffset ChosenSlotTime { get { return new DateTimeOffset(ChosenSlot, TimeSpan.Zero); } }
        /// <summary>
        /// Chosen place
        /// </summary>
        public string ChosenPlaceId { get { return visitor.ChosenPlaceId; } }
        /// <summary>
        /// Test result. Available options are in Model.TestResult
        /// </summary>
        public string Result { get { return visitor.Result; } }
        public DateTimeOffset? ResultNotifiedAt { get { return visitor.ResultNotifiedAt; } }
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet { get { return $"{cohash}{visitor.TestingSet}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Last change
        /// </summary>
        public DateTimeOffset LastUpdate { get { return visitor.LastUpdate; } }
        /// <summary>
        /// Last change
        /// </summary>
        public int? ResultNotifiedCount { get { return visitor.ResultNotifiedCount; } }
        /// <summary>
        /// Visitor has registered by himself
        /// </summary>
        public bool? SelfRegistration { get { return visitor.SelfRegistration; } }
        /// <summary>
        /// If administration worker changes the data we store this information
        /// </summary>
        public string RegistrationUpdatedByManager { get { return $"{cohash}{visitor.RegistrationUpdatedByManager}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Registration time
        /// </summary>
        public DateTimeOffset? RegistrationTime { get { return visitor.RegistrationTime; } }
        /// <summary>
        /// Real time when the test has been taken
        /// </summary>
        public DateTimeOffset? TestingTime { get { return visitor.TestingTime; } }
        /// <summary>
        /// Time, when visitor has clicked that he is in the queue
        /// </summary>
        public DateTimeOffset? Enqueued { get { return visitor.Enqueued; } }
        /// <summary>
        /// Product id
        /// </summary>
        public string Product { get { return visitor.Product; } }
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId { get { return $"{cohash}{visitor.VerificationId}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Captcha token. After it is used it is removed
        /// </summary>
        public string Token { get { return $"{cohash}{visitor.Token}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Time when test has been confirmed by lab
        /// </summary>
        public DateTimeOffset? TestResultTime { get { return visitor.TestResultTime; } }
        public DateTimeOffset? LastStatusCheck { get { return visitor.LastStatusCheck; } }
        /// <summary>
        /// Administration worker who has validated the person identity
        /// </summary>
        public string VerifiedBy { get { return $"{cohash}{visitor.VerifiedBy}".GetSHA256Hash().Substring(0, 10); } }
        /// <summary>
        /// Administration worker IP adddress
        /// </summary>
        public string VerifiedFromIP { get { return $"{cohash}{visitor.VerifiedFromIP}".GetSHA256Hash().Substring(0, 10); } }
    }
}
