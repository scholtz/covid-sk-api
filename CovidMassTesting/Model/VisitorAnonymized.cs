using CovidMassTesting.Helpers;
using System;

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
        public string Id => $"{cohash}{visitor.Id}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Covid pass
        /// </summary>
        public string PersonTrackingNumber => $"{cohash}{visitor.PersonTrackingNumber}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Gender F | M
        /// </summary>
        public string Gender => $"{cohash}{visitor.Gender}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Nationality
        /// </summary>
        public string Nationality => $"{cohash}{visitor.Nationality}".GetSHA256Hash().Substring(0, 10);
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
        public string Passport => $"{cohash}{visitor.Passport}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Personal number if person type is idcard or child
        /// </summary>
        public string RC => $"{cohash}{visitor.RC}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Employee id if applicable
        /// </summary>
        public string EmployeeId => $"{cohash}{visitor.EmployeeId}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// BirthDay - day
        /// </summary>
        public int? BirthDayDay => null;
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
        public string FirstName => $"{cohash}{visitor.FirstName}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName => $"{cohash}{visitor.LastName}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// ZIP - Pernament address
        /// </summary>
        public string ZIP => $"{cohash}{visitor.ZIP}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// City - Pernament address
        /// </summary>
        public string City => $"{cohash}{visitor.City}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Street - Pernament address
        /// </summary>
        public string Street => $"{cohash}{visitor.Street}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// StreetNo - Pernament address
        /// </summary>
        public string StreetNo => $"{cohash}{visitor.StreetNo}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Address - Pernament address
        /// </summary>
        public string Address => $"{cohash}{visitor.Address}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Email
        /// </summary>
        public string Email => $"{cohash}{visitor.Email}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Phone
        /// </summary>
        public string Phone => $"{cohash}{visitor.Phone}".GetSHA256Hash().Substring(0, 10);
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
        public DateTimeOffset ChosenSlotTime => (new DateTimeOffset(ChosenSlot, TimeSpan.Zero)).ToLocalTime();
        /// <summary>
        /// Chosen place
        /// </summary>
        public string ChosenPlaceId => visitor.ChosenPlaceId;
        /// <summary>
        /// Test result. Available options are in Model.TestResult
        /// </summary>
        public string Result => visitor.Result;
        /// <summary>
        /// Time when visitor has been notified by our notification methods
        /// </summary>
        public DateTimeOffset? ResultNotifiedAt => visitor.ResultNotifiedAt?.ToLocalTime();
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet => $"{cohash}{visitor.TestingSet}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Last change
        /// </summary>
        public DateTimeOffset LastUpdate => visitor.LastUpdate.ToLocalTime();
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
        public string RegistrationUpdatedByManager => $"{cohash}{visitor.RegistrationUpdatedByManager}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Registration time
        /// </summary>
        public DateTimeOffset? RegistrationTime => visitor.RegistrationTime?.ToLocalTime();
        /// <summary>
        /// Real time when the test has been taken
        /// </summary>
        public DateTimeOffset? TestingTime => visitor.TestingTime?.ToLocalTime();
        /// <summary>
        /// Time, when visitor has clicked that he is in the queue
        /// </summary>
        public DateTimeOffset? Enqueued => visitor.Enqueued?.ToLocalTime();
        /// <summary>
        /// Product id
        /// </summary>
        public string Product => visitor.Product;
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId => $"{cohash}{visitor.VerificationId}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Captcha token. After it is used it is removed
        /// </summary>
        public string Token => $"{cohash}{visitor.Token}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Time when test has been confirmed by lab
        /// </summary>
        public DateTimeOffset? TestResultTime => visitor.TestResultTime?.ToLocalTime();
        public DateTimeOffset? LastStatusCheck => visitor.LastStatusCheck?.ToLocalTime();

        /// <summary>
        /// Time when the result of the test was successfully sent to government system
        /// </summary>
        public DateTimeOffset? EHealthNotifiedAt => visitor.EHealthNotifiedAt?.ToLocalTime();
        /// <summary>
        /// Time when the user was downloaded from external system
        /// </summary>
        public DateTimeOffset? DownloadedAt => visitor.DownloadedAt?.ToLocalTime();
        /// <summary>
        /// Administration worker who has validated the person identity
        /// </summary>
        public string VerifiedBy => $"{cohash}{visitor.VerifiedBy}".GetSHA256Hash().Substring(0, 10);
        /// <summary>
        /// Administration worker IP adddress
        /// </summary>
        public string VerifiedFromIP => $"{cohash}{visitor.VerifiedFromIP}".GetSHA256Hash().Substring(0, 10);
    }
}
