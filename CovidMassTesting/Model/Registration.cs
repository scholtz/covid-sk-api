using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class Registration
    {
        /// <summary>
        /// Registration Identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Language in which we will communicate to the visitor
        /// 
        /// sk | en
        /// </summary>
        public string Language { get; set; } = "sk";
        /// <summary>
        /// Name
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; }
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
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Phone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Identifier within the company
        /// </summary>
        public List<CompanyIdentifier> CompanyIdentifiers { get; set; } = new List<CompanyIdentifier>();
        /// <summary>
        /// Time when registration has been created
        /// </summary>
        public DateTimeOffset? Created { get; set; }
        /// <summary>
        /// Time when registration has been last updated
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; }
    }
}
