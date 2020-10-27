using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public class User
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string[] Roles { get; set; }
        public string PswHash { get; set; }
        public string CoHash { get; set; }

        public UserPublic ToPublic()
        {
            return new UserPublic()
            {
                Email = Email,
                Name = Name,
                Roles = Roles
            };
        }
    }
}
