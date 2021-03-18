using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Visitor in specified timezone
    /// </summary>
    public class VisitorTimezoned
    {
        /// <summary>
        /// Parent visitor entity
        /// </summary>
        public readonly Visitor visitor;
        private readonly TimeSpan timezone;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="timezone"></param>
        public VisitorTimezoned(Visitor visitor, TimeSpan timezone)
        {
            this.visitor = visitor;
            this.timezone = timezone;
        }
        /// <summary>
        /// Registration code. 9-digit, formatted 000-000-000 for visitors
        /// </summary>
        public int Id => visitor.Id;
        /// <summary>
        /// Covid pass
        /// </summary>
        public string PersonTrackingNumber => visitor.PersonTrackingNumber;
        /// <summary>
        /// Gender F | M
        /// </summary>
        public string Gender => visitor.Gender;
        /// <summary>
        /// Nationality
        /// </summary>
        public string Nationality => visitor.Nationality;
        /// <summary>
        /// Language in which we will communicate to the visitor
        /// 
        /// sk | en
        /// </summary>
        public string Language => visitor.Language;
        /// <summary>
        /// Type of person
        /// 
        /// idcard|child|foreign
        /// </summary>
        public string PersonType => visitor.PersonType;
        /// <summary>
        /// Passport number if person type is foreigner
        /// </summary>
        public string Passport => visitor.Passport;
        /// <summary>
        /// Personal number if person type is idcard or child
        /// </summary>
        public string RC => visitor.RC;
        /// <summary>
        /// Employee id if applicable
        /// </summary>
        public string EmployeeId => visitor.EmployeeId;
        /// <summary>
        /// BirthDay - day
        /// </summary>
        public int? BirthDayDay => visitor.BirthDayDay;
        /// <summary>
        /// BirthDay - month
        /// </summary>
        public int? BirthDayMonth => visitor.BirthDayMonth;
        /// <summary>
        /// BirthDay - year
        /// </summary>
        public int? BirthDayYear => visitor.BirthDayYear;
        /// <summary>
        /// Name
        /// </summary>
        public string FirstName => visitor.FirstName;
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName => visitor.LastName;
        /// <summary>
        /// ZIP - Pernament address
        /// </summary>
        public string ZIP => visitor.ZIP;
        /// <summary>
        /// City - Pernament address
        /// </summary>
        public string City => visitor.City;
        /// <summary>
        /// Street - Pernament address
        /// </summary>
        public string Street => visitor.Street;
        /// <summary>
        /// StreetNo - Pernament address
        /// </summary>
        public string StreetNo => visitor.StreetNo;
        /// <summary>
        /// Address - Pernament address
        /// </summary>
        public string Address => visitor.Address;
        /// <summary>
        /// Email
        /// </summary>
        public string Email => visitor.Email;
        /// <summary>
        /// Phone
        /// </summary>
        public string Phone => visitor.Phone;
        /// <summary>
        /// Insurance
        /// </summary>
        public string Insurance => visitor.Insurance;
        /// <summary>
        /// Chosen time slot
        /// </summary>
        public long ChosenSlot => visitor.ChosenSlot;
        /// <summary>
        /// ChosenSlotTime
        /// </summary>
        public DateTimeOffset ChosenSlotTime => (new DateTimeOffset(ChosenSlot, TimeSpan.Zero)).ToOffset(timezone);
        /// <summary>
        /// Chosen place
        /// </summary>
        public string ChosenPlaceId => visitor.ChosenPlaceId;

        /// <summary>
        /// Place name
        /// </summary>
        public string PlaceName => visitor.PlaceName;
        /// <summary>
        /// PlaceProviderId
        /// </summary>
        public string PlaceProviderId => visitor.PlaceProviderId;
        /// <summary>
        /// Test result. Available options are in Model.TestResult
        /// </summary>
        public string Result => visitor.Result;
        /// <summary>
        /// Time when visitor has been notified by our notification methods
        /// </summary>
        public DateTimeOffset? ResultNotifiedAt => visitor.ResultNotifiedAt?.ToOffset(timezone);
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet => visitor.TestingSet;
        /// <summary>
        /// Last change
        /// </summary>
        public DateTimeOffset LastUpdate => visitor.LastUpdate.ToOffset(timezone);
        /// <summary>
        /// Last change
        /// </summary>
        public int? ResultNotifiedCount => visitor.ResultNotifiedCount;
        /// <summary>
        /// Visitor has registered by himself
        /// </summary>
        public bool? SelfRegistration => visitor.SelfRegistration;
        /// <summary>
        /// If administration worker changes the data we store this information
        /// </summary>
        public string RegistrationUpdatedByManager => visitor.RegistrationUpdatedByManager;
        /// <summary>
        /// Registration time
        /// </summary>
        public DateTimeOffset? RegistrationTime => visitor.RegistrationTime?.ToOffset(timezone);
        /// <summary>
        /// Real time when the test has been taken
        /// </summary>
        public DateTimeOffset? TestingTime => visitor.TestingTime?.ToOffset(timezone);
        /// <summary>
        /// Time, when visitor has clicked that he is in the queue
        /// </summary>
        public DateTimeOffset? Enqueued => visitor.Enqueued?.ToOffset(timezone);
        /// <summary>
        /// Product id
        /// </summary>
        public string Product => visitor.Product;
        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName => visitor.ProductName;
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId => visitor.VerificationId;
        /// <summary>
        /// Captcha token. After it is used it is removed
        /// </summary>
        public string Token => visitor.Token;
        /// <summary>
        /// Time when test has been confirmed by lab
        /// </summary>
        public DateTimeOffset? TestResultTime => visitor.TestResultTime?.ToOffset(timezone);
        /// <summary>
        /// Last status
        /// </summary>
        public DateTimeOffset? LastStatusCheck => visitor.LastStatusCheck?.ToOffset(timezone);

        /// <summary>
        /// Time when the result of the test was successfully sent to government system
        /// </summary>
        public DateTimeOffset? EHealthNotifiedAt => visitor.EHealthNotifiedAt?.ToOffset(timezone);
        /// <summary>
        /// Time when the user was downloaded from external system
        /// </summary>
        public DateTimeOffset? DownloadedAt => visitor.DownloadedAt?.ToOffset(timezone);
        /// <summary>
        /// Administration worker who has validated the person identity
        /// </summary>
        public string VerifiedBy => visitor.VerifiedBy;
        /// <summary>
        /// Administration worker IP adddress
        /// </summary>
        public string VerifiedFromIP => visitor.VerifiedFromIP;
    }
}
