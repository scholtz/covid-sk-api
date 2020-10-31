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
        public class Claims
        {
            public const string Role = "Role";
            public const string Name = "Name";
            public const string Email = ClaimTypes.NameIdentifier;
        }
        public class Groups
        {
            public const string Admin = "Admin";
            public const string PasswordProtected = "PasswordProtected";
            public const string RegistrationManager = "RegistrationManager";
            public const string MedicTester = "MedicTester";
            public const string DocumentManager = "DocumentManager";
            public const string MedicLab = "MedicLab";
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.FirstOrDefault(c => c.Type == Claims.Email)?.Value ?? "";
        }
        public static string GetName(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.FirstOrDefault(c => c.Type == Claims.Name)?.Value ?? "";
        }
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == Claims.Role && c.Value == Groups.Admin);
        }
        public static bool IsPasswordProtected(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == Claims.Role && c.Value == Groups.PasswordProtected);
        }

        public static bool IsRegistrationManager(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == Claims.Role && (c.Value == Groups.Admin || c.Value == Groups.RegistrationManager));
        }
        public static bool IsMedicTester(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == Claims.Role && (c.Value == Groups.Admin || c.Value == Groups.MedicTester));
        }
        public static bool IsMedicLab(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == Claims.Role && (c.Value == Groups.Admin || c.Value == Groups.MedicLab));
        }
        public static bool IsDocumentManager(this ClaimsPrincipal user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return user.Claims.Any(c => c.Type == Claims.Role && (c.Value == Groups.Admin || c.Value == Groups.DocumentManager));
        }
        private static JwtSecurityToken Parse(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadToken(token) as JwtSecurityToken;
        }
    }
}
