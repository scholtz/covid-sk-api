using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CovidMassTesting.Model
{
    public static class Token
    {
        public const string RoleClaim = "Role";
        public const string EmailClaim = "Email";


        public static string GetEmail(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.FirstOrDefault(c => c.Type == EmailClaim)?.Value ?? "";
        }
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == RoleClaim && c.Value == "Admin");
        }
        public static bool IsRegistrationManager(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == RoleClaim && c.Value == "Admin" || c.Value == "RegistrationManager");
        }
        public static bool IsMedicTester(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == RoleClaim && c.Value == "Admin" || c.Value == "MedicTester");
        }
        public static bool IsMedicLab(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == RoleClaim && c.Value == "Admin" || c.Value == "MedicLab");
        }
        public static bool IsDocumentManager(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == RoleClaim && c.Value == "Admin" || c.Value == "DocumentManager");
        }
        private static JwtSecurityToken Parse(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }
    }
}
