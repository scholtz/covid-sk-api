using CovidMassTesting.Repository.Interface;
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
            public const string Email = "nameid";
            /// <summary>
            /// Place provider claim identifier
            /// </summary>
            public const string PlaceProvider = "pp";
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

            return user.Claims.FirstOrDefault(c => c.Type == Claims.Email || c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
        }
        /// <summary>
        /// Get place provider from claim
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string GetPlaceProvider(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.FirstOrDefault(c => c.Type == Claims.PlaceProvider)?.Value ?? "";
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
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsAdmin(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.Admin }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Returns true if the user is currently in the role of PP Admin.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <returns></returns>
        public static async Task<bool> IsPlaceProviderAdmin(this ClaimsPrincipal user, IUserRepository userRepository, IPlaceProviderRepository placeProviderRepository)
        {
            if (user.IsAdmin(userRepository)) return true;
            var pp = GetPlaceProvider(user);
            return await placeProviderRepository.InAnyGroup(user.GetEmail(), pp, new string[] { Groups.PPAdmin });
        }

        /// <summary>
        /// Returns true if the user is currently in the role of PP Admin and acts on behalf of specific place.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public static async Task<bool> IsPlaceAdmin(
            this ClaimsPrincipal user,
            IUserRepository userRepository,
            IPlaceProviderRepository placeProviderRepository,
            IPlaceRepository placeRepository,
            string placeId
            )
        {
            var place = await placeRepository.GetPlace(placeId);
            if (place == null) return false;
            if (user.IsAdmin(userRepository)) return true;
            var pp = GetPlaceProvider(user);
            if (pp != place.PlaceProviderId) return false;
            return await placeProviderRepository.InAnyGroup(user.GetEmail(), pp, new string[] { Groups.PPAdmin });
        }


        /// <summary>
        /// Check if user has password protected .. Created for demo users
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsPasswordProtected(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.PasswordProtected }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Checks if user has role Registration Manager
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsRegistrationManager(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.Admin, Groups.RegistrationManager }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Checks if user has role Medic Tester
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsMedicTester(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.Admin, Groups.MedicTester }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Checks if user has role Medic Lab
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsMedicLab(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.Admin, Groups.MedicLab }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Check if user has role Document Manager
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsDocumentManager(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.Admin, Groups.DocumentManager }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Check if user has role Data exporter
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <returns></returns>
        public static bool IsDataExporter(this ClaimsPrincipal user, IUserRepository userRepository)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }

            var email = user.GetEmail();
            return userRepository.InAnyGroup(email, new string[] { Groups.DataExporter }, user.GetPlaceProvider()).Result;
        }
        /// <summary>
        /// Accountant or admin is authorized to issue invoice
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public static bool IsAuthorizedToIssueInvoice(this ClaimsPrincipal user, IUserRepository userRepository, IPlaceProviderRepository placeProviderRepository, string placeProviderId)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }
            if (placeProviderRepository is null)
            {
                throw new ArgumentNullException(nameof(placeProviderRepository));
            }
            if (string.IsNullOrEmpty(placeProviderId))
            {
                throw new ArgumentNullException(nameof(placeProviderId));
            }

            var email = user.GetEmail();

            return placeProviderRepository.InAnyGroup(email, placeProviderId, new string[] { Groups.Admin, Groups.Accountant }).Result;
        }
        /// <summary>
        /// Log in as company
        /// </summary>
        /// <param name="user"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public static async Task<bool> IsAuthorizedToLogAsCompany(this ClaimsPrincipal user, IUserRepository userRepository, IPlaceProviderRepository placeProviderRepository, string placeProviderId)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (userRepository is null)
            {
                throw new ArgumentNullException(nameof(userRepository));
            }
            if (user.IsAdmin(userRepository))
            {
                return true;
            }

            if (placeProviderRepository is null)
            {
                throw new ArgumentNullException(nameof(placeProviderRepository));
            }
            if (string.IsNullOrEmpty(placeProviderId))
            {
                throw new ArgumentNullException(nameof(placeProviderId));
            }

            var email = user.GetEmail();

            var pp = await placeProviderRepository.GetPlaceProvider(placeProviderId);
            if (pp == null) return false;
            if (pp.Users?.Any(u => u.Email == email) == true) return true;
            return await placeProviderRepository.InAnyGroup(email, placeProviderId, new string[] { Groups.Admin, Groups.PPAdmin, Groups.Accountant, Groups.DataExporter, Groups.DocumentManager, Groups.MedicLab, Groups.MedicTester, Groups.RegistrationManager });
        }
        /// <summary>
        /// Method creates jwt token
        /// </summary>
        /// <param name="usr">User object</param>
        /// <param name="configuration">APP Configuran</param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public static string CreateToken(User usr, IConfiguration configuration, string placeProviderId)
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
                    Token.Claims.PlaceProvider,
                     placeProviderId
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
