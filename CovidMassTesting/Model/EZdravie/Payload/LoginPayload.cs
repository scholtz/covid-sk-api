using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie
{
    public class LoginPayload
    {
        public string[] ApplicationRoles { get; set; }
        public Dictionary<string, string> ResourceRights { get; set; }
        public SessionPayload Session { get; set; }
        public UserPayload User { get; set; }
    }
}
