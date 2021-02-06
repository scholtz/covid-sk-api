using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// User model
    /// </summary>
    public class User
    {
        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// User phone
        /// </summary>
        public string Phone { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// List of roles
        /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
        public List<string> Roles { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists
        /// <summary>
        /// Password
        /// </summary>
        public string PswHash { get; set; }
        /// <summary>
        /// Cohash
        /// </summary>
        public string CoHash { get; set; }
        /// <summary>
        /// CoData
        /// </summary>
        public string CoData { get; set; }
        /// <summary>
        /// Last invalid password attempt
        /// </summary>
        public DateTimeOffset? InvalidLogin { get; set; }
        /// <summary>
        /// Place at which person is assigned. All person's registrations will be placed to this location
        /// </summary>
        public string Place { get; set; }
        /// <summary>
        /// Last time user has updated his location
        /// </summary>
        public DateTimeOffset? PlaceLastCheck { get; set; }

        /// <summary>
        /// Converts to public export (password is not sent out)
        /// </summary>
        /// <returns></returns>
        public UserPublic ToPublic()
        {
            return new UserPublic()
            {
                Email = Email,
                Name = Name,
                Roles = Roles,
                Place = Place,
                PlaceLastCheck = PlaceLastCheck
            };
        }
    }
}
