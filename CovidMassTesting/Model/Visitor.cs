using System;
using System.Collections.Generic;

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
        /// Covid pass
        /// </summary>
        public string PersonTrackingNumber { get; set; }
        /// <summary>
        /// Gender F | M
        /// </summary>
        public string Gender { get; set; }
        /// <summary>
        /// Nationality
        /// </summary>
        public string Nationality { get; set; }
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
        /// Employee id if applicable
        /// </summary>
        public string EmployeeId { get; set; }
        /// <summary>
        /// BirthDay - day
        /// </summary>
        public int? BirthDayDay { get; set; }
        /// <summary>
        /// BirthDay - month
        /// </summary>
        public int? BirthDayMonth { get; set; }
        /// <summary>
        /// BirthDay - year
        /// </summary>
        public int? BirthDayYear { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// ZIP - Pernament address
        /// </summary>
        public string ZIP { get; set; }
        /// <summary>
        /// City - Pernament address
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Street - Pernament address
        /// </summary>
        public string Street { get; set; }
        /// <summary>
        /// StreetNo - Pernament address
        /// </summary>
        public string StreetNo { get; set; }
        /// <summary>
        /// Address - Pernament address
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
        /// ChosenSlotTime
        /// </summary>
        public DateTimeOffset ChosenSlotTime => new DateTimeOffset(ChosenSlot, TimeSpan.Zero);
        /// <summary>
        /// Chosen place
        /// </summary>
        public string ChosenPlaceId { get; set; }
        /// <summary>
        /// Name of the place
        /// </summary>
        public string PlaceName { get; set; }
        /// <summary>
        /// ID of place provider
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Test result. Available options are in Model.TestResult
        /// </summary>
        public string Result { get; set; } = TestResult.NotTaken;
        /// <summary>
        /// Time when visitor has been notified with his status
        /// </summary>
        public DateTimeOffset? ResultNotifiedAt { get; set; } = null;
        /// <summary>
        /// Testing set identifier
        /// </summary>
        public string TestingSet { get; set; }
        /// <summary>
        /// Last change
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }
        /// <summary>
        /// Last change
        /// </summary>
        public int? ResultNotifiedCount { get; set; }
        /// <summary>
        /// Visitor has registered by himself
        /// </summary>
        public bool? SelfRegistration { get; set; }
        /// <summary>
        /// If administration worker changes the data we store this information
        /// </summary>
        public string RegistrationUpdatedByManager { get; set; }
        /// <summary>
        /// Registration time
        /// </summary>
        public DateTimeOffset? RegistrationTime { get; set; }
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
        /// Name of the place
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// Verification id is used to share the test results with others. It does not contain any sensitive data such as personal number, but it contains information when the visitor has taken the test with the test result and his name.
        /// </summary>
        public string VerificationId { get; set; }
        /// <summary>
        /// Captcha token. After it is used it is removed
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Time when test has been confirmed by lab
        /// </summary>
        public DateTimeOffset? TestResultTime { get; set; }
        /// <summary>
        /// Time when user has checked his status last time
        /// </summary>
        public DateTimeOffset? LastStatusCheck { get; set; }
        /// <summary>
        /// Time when the result of the test was successfully sent to government system
        /// </summary>
        public DateTimeOffset? EHealthNotifiedAt { get; set; }
        /// <summary>
        /// Time when the user was downloaded from external system
        /// </summary>
        public DateTimeOffset? DownloadedAt { get; set; }
        /// <summary>
        /// Administration worker who has validated the person identity
        /// </summary>
        public string VerifiedBy { get; set; }
        /// <summary>
        /// Administration worker IP adddress
        /// </summary>
        public string VerifiedFromIP { get; set; }

        internal void Extend(Dictionary<string, Place> places, Dictionary<string, Product> products)
        {
            if (places.ContainsKey(this.ChosenPlaceId))
            {
                this.PlaceName = places[this.ChosenPlaceId].Name;
            }
            if (products.ContainsKey(this.Product))
            {
                this.ProductName = products[this.Product].Name;
            }
        }
    }
}
