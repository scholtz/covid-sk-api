using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// List of roles/groups
    /// </summary>
    public static class Groups
    {
        /// <summary>
        /// Admin can create users, places, set dates, and all methods with other roles
        /// </summary>
        public const string Admin = "Admin";
        /// <summary>
        /// Place provider Admin can create users, places, set dates, and all methods with other roles within scope of the place provider
        /// </summary>
        public const string PPAdmin = "PPAdmin";
        /// <summary>
        /// Accountant is able to list invoices to the hospital or issue new one
        /// </summary>
        public const string Accountant = "Accountant";
        /// <summary>
        /// User with this role can not change password
        /// </summary>
        public const string PasswordProtected = "PasswordProtected";
        /// <summary>
        /// User in this role can fetch users by the registration code
        /// </summary>
        public const string RegistrationManager = "RegistrationManager";
        /// <summary>
        /// User with this role can assign test bar code to the registed user
        /// </summary>
        public const string MedicTester = "MedicTester";
        /// <summary>
        /// User with this role can export data of all infected users and pass them to covid center
        /// </summary>
        public const string DocumentManager = "DocumentManager";
        /// <summary>
        /// User with this role can set testing results
        /// </summary>
        public const string MedicLab = "MedicLab";
        /// <summary>
        /// Person in this group is allowed to list all sick people and export data
        /// </summary>
        public const string DataExporter = "DataExporter";

        /// <summary>
        /// Validates input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool ValidateGroupName(this string input)
        {
            switch (input)
            {
                case Admin:
                case Accountant:
                case PasswordProtected:
                case RegistrationManager:
                case MedicLab:
                case MedicTester:
                case DataExporter:
                    return true;
            }
            return false;
        }
    }
}
