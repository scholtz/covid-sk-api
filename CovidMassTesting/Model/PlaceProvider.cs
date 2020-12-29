using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Place provider is hospital or company prividing testing place. One hospital can have multiple testing places.
    /// </summary>
    public class PlaceProvider
    {
        /// <summary>
        /// ID 
        /// </summary>
        public string PlaceProviderId { get; set; }
        /// <summary>
        /// Hospital name
        /// </summary>
        public string CompanyName { get; set; }
        /// <summary>
        /// hospital/company registration identifier from trade registry
        /// </summary>
        public string CompanyId { get; set; }
        /// <summary>
        /// VAT identifier
        /// </summary>
        public string VAT { get; set; }
        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Admin email
        /// </summary>
        public string MainEmail { get; set; }
        /// <summary>
        /// Email is confirmed
        /// </summary>
        public bool MainEmailConfirmed { get; set; }
        /// <summary>
        /// Phone
        /// </summary>
        public string PrivatePhone { get; set; }
        /// <summary>
        /// Phone confirmed by sms
        /// </summary>
        public bool PhoneConfirmed { get; set; }
        /// <summary>
        /// Public email
        /// </summary>
        public string PublicEmail { get; set; }
        /// <summary>
        /// Public phone
        /// </summary>
        public string PublicPhone { get; set; }
        /// <summary>
        /// Web
        /// </summary>
        public string Web { get; set; }
        /// <summary>
        /// Logo in base64
        /// </summary>
        public string Logo { get; set; }
        /// <summary>
        /// Custom CSS
        /// </summary>
        public string CSS { get; set; }
        /// <summary>
        /// Users in groups. Dictionary key is the group id, value is list of users in that group
        /// </summary>
        public Dictionary<string, List<string>> Group2Emails { get; set; } = new Dictionary<string, List<string>>();
        /// <summary>
        /// Place provider private main contact name
        /// </summary>
        public string MainContact { get; set; }
        /// <summary>
        /// List of accepted invitations - users who can hr manage to places
        /// </summary>
        public List<Invitation> Users { get; set; }
        /// <summary>
        /// List of person allocations
        /// </summary>
        public List<PersonAllocation> Allocations { get; set; } = new List<PersonAllocation>();
        /// <summary>
        /// List of products served by place provider
        /// </summary>
        public List<Product> Products { get; set; } = new List<Product>();

        /// <summary>
        /// Convert to public info
        /// </summary>
        /// <returns></returns>
        public PlaceProviderPublic ToPublic()
        {
            return new PlaceProviderPublic()
            {
                CompanyName = this.CompanyName,
                PublicEmail = this.PublicEmail,
                PublicPhone = this.PublicPhone,
                Logo = this.Logo,
                CSS = this.CSS,
                Web = this.Web,
                Country = this.Country,
            };
        }
    }
    /// <summary>
    /// Public place provider information
    /// </summary>
    public class PlaceProviderPublic
    {
        /// <summary>
        /// Company name
        /// </summary>
        public string CompanyName { get; set; }
        /// <summary>
        /// Country
        /// </summary>
        public string Country { get; internal set; }
        /// <summary>
        /// Email
        /// </summary>
        public string PublicEmail { get; internal set; }
        /// <summary>
        /// Phone
        /// </summary>
        public string PublicPhone { get; internal set; }
        /// <summary>
        /// Logo
        /// </summary>
        public string Logo { get; internal set; }
        /// <summary>
        /// CSS
        /// </summary>
        public string CSS { get; internal set; }
        /// <summary>
        /// Web
        /// </summary>
        public string Web { get; internal set; }
    }
}
