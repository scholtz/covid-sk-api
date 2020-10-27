using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    public class UserRepository : IUserRepository
    {
        private readonly ILogger<UserRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IEmailSender emailSender;
        private readonly IConfiguration configuration;
        private readonly string REDIS_KEY_USERS_OBJECTS = "PLACE";

        private readonly int RehashN = 99;

        public UserRepository(
            IConfiguration configuration,
            ILogger<UserRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.emailSender = emailSender;
            this.configuration = configuration;
        }
        public async Task<bool> Add(User user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            (string pass, string hash, string cohash) = GeneratePassword();
            user.PswHash = hash;
            user.CoHash = cohash;
            await emailSender.SendEmail(user.Email, user.Name, new Model.Email.InvitationEmail()
            {
                Name = user.Name,
                Password = pass,
                Roles = user.Roles,
            });
            return await Set(user);
        }
        private (string pass, string hash, string cohash) GeneratePassword()
        {
            var orig = GenerateRandomPassword();
            var pass = orig.Clone().ToString();
            var cohash = GenerateRandomPassword(new PasswordOptions() { RequiredLength = 4 });

            for (int i = 0; i < RehashN; i++)
            {
                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
            }

            return (orig, pass, cohash);
        }
        /// <summary>
        /// Generates a Random Password
        /// respecting the given strength requirements.
        /// </summary>
        /// <param name="opts">A valid PasswordOptions object
        /// containing the password strength requirements.</param>
        /// <returns>A random password</returns>
        public static string GenerateRandomPassword(PasswordOptions opts = null)
        {
            if (opts == null) opts = new PasswordOptions()
            {
                RequiredLength = 10,
                RequiredUniqueChars = 4,
                RequireDigit = true,
                RequireLowercase = true,
                RequireNonAlphanumeric = true,
                RequireUppercase = true
            };

            string[] randomChars = new[] {
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
            "abcdefghijkmnopqrstuvwxyz",    // lowercase
            "0123456789",                   // digits
            "!@$?_-"                        // non-alphanumeric
        };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count),
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count),
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }

        public virtual async Task<bool> Set(User user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            logger.LogInformation($"Setting user {user.Email}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (!await redisCacheClient.Db0.HashSetAsync(REDIS_KEY_USERS_OBJECTS, user.Email, encoded, true))
            {
                throw new Exception("Error creating record in the database");
            }
            return true;
        }
        public virtual async Task<User> Get(string email)
        {
            logger.LogInformation($"User loaded from database: {email}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>(REDIS_KEY_USERS_OBJECTS, email);
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<User>(decoded);
        }
        public virtual Task<IEnumerable<User>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<User>(REDIS_KEY_USERS_OBJECTS);
        }

        public async Task CreateAdminUsersFromConfiguration()
        {
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();//.GetValue<List<CovidMassTesting.Model.Settings.User>>("AdminUsers");
            if (users == null)
            {
                logger.LogInformation("No admin users are defined in the configuration");
                return;
            }
            foreach (var usr in users)
            {
                try
                {
                    var user = await Get(usr.Email);
                    if (user == null)
                    {
                        await Add(new User()
                        {
                            Email = usr.Email,
                            Roles = new string[] { "Admin" },
                            Name = usr.Name
                        });
                    }
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, exc.Message);
                }
            }
        }
        /// <summary>
        /// Returns cohash
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<string> Preauthenticate(string email)
        {
            return (await Get(email))?.CoHash;
        }
        /// <summary>
        /// Returns JWT token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> Authenticate(string email, string hash, string data)
        {
            var usr = await Get(email);
            var ourHash = Encoding.ASCII.GetBytes($"{usr.PswHash}{data}").GetSHA256Hash();
            if (ourHash != hash)
            {
                throw new Exception("Invalid user or password");
            }


            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["JWTTokenSecret"]);
            ClaimsIdentity subject;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = subject = new ClaimsIdentity(new Claim[]
                {
                        new Claim(ClaimTypes.NameIdentifier, email),
                        new Claim("Name",usr.Name)
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            foreach (var role in usr.Roles)
            {
                subject.AddClaim(new Claim("Role", role));
            }
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
