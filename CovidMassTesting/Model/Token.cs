using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    /// <summary>
    /// Token management
    /// </summary>
    public static class Token
    {
        /// <summary>
        /// Claim names
        /// </summary>
        internal static class Claims
        {
            /// <summary>
            /// Roles claim identifier
            /// </summary>
            public const string Role = "Role";
            /// <summary>
            /// Name claim identifier
            /// </summary>
            public const string Name = "Name";
            /// <summary>
            /// Email claim identifier
            /// </summary>
            public const string Email = ClaimTypes.NameIdentifier;
        }
        /// <summary>
        /// List of roles
        /// </summary>
        internal static class Groups
        {
            /// <summary>
            /// Admin can create users, places, set dates, and all methods with other roles
            /// </summary>
            public const string Admin = "Admin";
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
        }
        /// <summary>
        /// Get email from claim
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string GetEmail(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.FirstOrDefault(c => c.Type == Claims.Email)?.Value ?? "";
        }
        /// <summary>
        /// Get name from claim
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string GetName(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.FirstOrDefault(c => c.Type == Claims.Name)?.Value ?? "";
        }
        /// <summary>
        /// Checks if user is admin
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roles = ProcessRoles(user);
            return roles.Any(c => c == Groups.Admin);
        }
        /// <summary>
        /// Roles from claim
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string[] ProcessRoles(ClaimsPrincipal user)
        {
            var value = user.Claims.Where(c => c.Type == Claims.Role).Select(c=>c.Value).ToArray();
            if (value == null)
            {
                return Array.Empty<string>();
            }
            return value;
        }
        /// <summary>
        /// Check if user has password protected .. Created for demo users
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsPasswordProtected(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roles = ProcessRoles(user);
            return roles.Any(c => c == Groups.PasswordProtected);
        }
        /// <summary>
        /// Checks if user has role Registration Manager
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsRegistrationManager(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roles = ProcessRoles(user);
            return roles.Any(c => c == Groups.Admin || c == Groups.RegistrationManager);
        }
        /// <summary>
        /// Checks if user has role Medic Tester
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsMedicTester(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roles = ProcessRoles(user);
            return roles.Any(c => c == Groups.Admin || c == Groups.MedicTester);
        }
        /// <summary>
        /// Checks if user has role Medic Lab
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsMedicLab(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roles = ProcessRoles(user);
            return roles.Any(c => c == Groups.Admin || c == Groups.MedicLab);
        }
        /// <summary>
        /// Check if user has role Document Manager
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool IsDocumentManager(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var roles = ProcessRoles(user);
            return roles.Any(c => c == Groups.Admin || c == Groups.DocumentManager);
        }
        private static JwtSecurityToken Parse(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }
        /// <summary>
        /// Method creates jwt token
        /// </summary>
        /// <param name="usr"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static string CreateToken(User usr, IConfiguration configuration)
        {
            if (usr is null)
            {
                throw new ArgumentNullException(nameof(usr));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var key = Encoding.ASCII.GetBytes(configuration["JWTTokenSecret"]);

            var payload = new JwtPayload {
                  {
                    Token.Claims.Email,
                     usr.Email
                  },{
                    Token.Claims.Name,
                     usr.Name
                  },
                  {
                    Token.Claims.Role,
                     usr.Roles
                  },
                  {
                    "nbf",
                     DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds()
                  },
                  {
                    "iat",
                     DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds()
                  },
                  {
                    "exp",
                     DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
                  },
            };

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

            var header = new JwtHeader(credentials);
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(secToken);
        }
    }
}
