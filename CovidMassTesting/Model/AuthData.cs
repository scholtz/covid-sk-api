using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Preauthenticate data to be hashed with password
    /// </summary>
    public class AuthData
    {
        /// <summary>
        /// Cohash is stable and is set when the password is originally set
        /// </summary>
        public string CoHash { get; set; }
        /// <summary>
        /// CoData is unique per sign in request. Server generated
        /// </summary>
        public string CoData { get; set; }
    }
}
