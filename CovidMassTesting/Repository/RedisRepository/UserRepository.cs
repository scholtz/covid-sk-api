using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Model.SMS;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
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
    /// <summary>
    /// User repository manages users and stores user data securly in the database
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ILogger<UserRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IEmailSender emailSender;
        private readonly ISMSSender smsSender;
        private readonly IConfiguration configuration;
        private readonly IPlaceRepository placeRepository;
        private readonly string REDIS_KEY_USERS_OBJECTS = "USERS";

        private readonly int RehashN = 99;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        public UserRepository(
            IConfiguration configuration,
            ILogger<UserRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.emailSender = emailSender;
            this.smsSender = smsSender;
            this.configuration = configuration;
            this.placeRepository = placeRepository;
        }
        /// <summary>
        /// Inserts new user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
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
                Roles = user.Roles.ToArray(),
            });
            if (!string.IsNullOrEmpty(user.Phone))
            {
                await smsSender.SendSMS(user.Phone, new NewUserSMS() { User = user.Name });
            }
            return await Set(user, true);
        }
        private (string pass, string hash, string cohash) GeneratePassword()
        {
            var orig = GenerateRandomPassword();
            var pass = orig.Clone().ToString();
            var cohash = GenerateRandomPassword();

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
        /// <summary>
        /// Set user. Encodes and stores to db.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public virtual async Task<bool> Set(User user, bool mustBeNew)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(user);
            logger.LogInformation($"Setting user {user.Email}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}", user.Email, encoded, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception("Error creating record in the database");
            }
            return true;
        }
        /// <summary>
        /// Removes user.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual async Task<bool> Remove(string email)
        {
            if (email is null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            logger.LogInformation($"Removing user {email}");
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}", email);
            return true;
        }
        /// <summary>
        /// Decode encrypted user data
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public virtual async Task<User> Get(string email)
        {
            logger.LogInformation($"User loaded from database: {email}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}", email);
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<User>(decoded);
        }
        /// <summary>
        /// Lists all users 
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<User>> ListAll()
        {
            var ret = new List<User>();
            var list = await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}");
            foreach (var item in list)
            {
                try
                {
                    ret.Add(await Get(item));
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Unable to deserialize user: {item}");
                }
            }
            return ret;
        }
        /// <summary>
        /// Registration manager can set his place at which he performs tests
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public async Task<bool> SetLocation(string email, string placeId)
        {
            if (string.IsNullOrEmpty(placeId)) throw new Exception("Invalid place provided");
            var place = await placeRepository.GetPlace(placeId);
            if (place == null) throw new Exception("Invalid place provided");
            var user = await Get(email);
            user.Place = placeId;
            return await Set(user, false);
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
                        if (string.IsNullOrEmpty(usr.Password))
                        {
                            await Add(new User()
                            {
                                Email = usr.Email,
                                Phone = usr.Phone,
                                Roles = usr.Roles == null ? new List<string>() { "Admin" } : usr.Roles.ToList(),
                                Name = usr.Name
                            });
                        }
                        else
                        {
                            var pass = usr.Password;
                            var cohash = GenerateRandomPassword();

                            for (int i = 0; i < RehashN; i++)
                            {
                                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
                            }

                            var newUser = new User()
                            {
                                Email = usr.Email,
                                Roles = usr.Roles == null ? new List<string>() { "Admin" } : usr.Roles.ToList(),
                                Name = usr.Name,
                                CoHash = cohash,
                                PswHash = pass
                            };
                            await Set(newUser, true);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(usr.Password))
                        {
                            // if password is defined (for testing), reset it on every app restart to config default

                            var pass = usr.Password;
                            var cohash = GenerateRandomPassword();

                            for (int i = 0; i < RehashN; i++)
                            {
                                pass = Encoding.ASCII.GetBytes($"{pass}{cohash}").GetSHA256Hash();
                            }

                            var newUser = new User()
                            {
                                Email = usr.Email,
                                Roles = usr.Roles == null ? new List<string>() { "Admin" } : usr.Roles.ToList(),
                                Name = usr.Name,
                                CoHash = cohash,
                                PswHash = pass
                            };
                            await Set(newUser, false);
                        }
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
        public async Task<AuthData> Preauthenticate(string email)
        {
            var user = await Get(email);
            if (user == null)
            {
                // invalid email .. we do not dispose usage of email on first sight so return valid answer

                return new AuthData()
                {
                    CoData = GenerateRandomPassword(),
                    CoHash = GenerateRandomPassword()
                };
            }
            user = await GenerateCoData(user);
            return new AuthData()
            {
                CoData = user.CoData,
                CoHash = user.CoHash
            };
        }

        private async Task<User> GenerateCoData(User user)
        {
            user.CoData = GenerateRandomPassword();
            await Set(user, false);
            return user;
        }
        private async Task<User> SetInvalidLogin(User user)
        {
            user.InvalidLogin = DateTimeOffset.UtcNow;
            await Set(user, false);
            return user;
        }
        /// <summary>
        /// Returns JWT token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> Authenticate(string email, string hash)
        {
            var usr = await Get(email);
            if (usr == null)
            {
                throw new Exception("Invalid user or password");
            }
            if (usr.InvalidLogin.HasValue && usr.InvalidLogin.Value.AddSeconds(1) > DateTimeOffset.UtcNow)
            {
                await SetInvalidLogin(usr);
                throw new Exception("Invalid user or password");
            }
            var ourHash = Encoding.ASCII.GetBytes($"{usr.PswHash}{usr.CoData}").GetSHA256Hash();
            if (ourHash != hash)
            {
                await SetInvalidLogin(usr);
                throw new Exception("Invalid user or password");
            }

            return Token.CreateToken(usr, configuration);
        }

        /// <summary>
        /// Change password
        /// 
        /// If successful, returns new JWT token
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="oldHash">Old password</param>
        /// <param name="newHash">New password</param>
        /// <returns></returns>
        public async Task<string> ChangePassword(string email, string oldHash, string newHash)
        {
            var user = await Get(email);
            if (user == null) throw new Exception("User not found by email");
            if (user.PswHash != oldHash) throw new Exception("Invalid old password");
            user.PswHash = newHash;
            user.CoHash = user.CoData;
            if (await Set(user, false))
            {
                return Token.CreateToken(user, configuration);
            }
            return "";
        }
        /// <summary>
        /// Checks if user with specified email has any of reqested groups
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="roles">Search any of this roles</param>
        /// <returns></returns>
        public async Task<bool> InAnyGroup(string email, string[] roles)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty", nameof(email));
            }

            if (roles is null)
            {
                throw new ArgumentNullException(nameof(roles));
            }

            if (roles.Length == 0) return true;


            var usr = await Get(email);
            foreach (var role in usr.Roles.ToArray())
            {
                if (role.Contains(","))
                {
                    foreach (var addrole in role.Split(','))
                    {
                        if (!usr.Roles.Contains(addrole))
                        {
                            usr.Roles.Add(addrole);
                        }
                    }
                }
                if (usr.Roles.Contains(role)) return true;
            }

            foreach (var role in roles)
            {
                if (usr.Roles.Contains(role)) return true;
            }
            return false;
        }
        /// <summary>
        /// Returns public information about specific user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<UserPublic> GetPublicUser(string email)
        {
            return (await Get(email)).ToPublic();
        }
        public async Task<bool> DropDatabaseAuthorize(string email, string hash)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException($"'{nameof(email)}' cannot be null or empty", nameof(email));
            }

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException($"'{nameof(hash)}' cannot be null or empty", nameof(hash));
            }
            var user = await Get(email);
            if (user == null) throw new Exception("User not found by email");
            if (user.InvalidLogin.HasValue && user.InvalidLogin.Value.AddSeconds(1) > DateTimeOffset.UtcNow)
            {
                await SetInvalidLogin(user);
                throw new Exception("Invalid user or password or attempt to login too fast after failed attempt");
            }
            var ourHash = Encoding.ASCII.GetBytes($"{user.PswHash}{user.CoData}").GetSHA256Hash();
            if (ourHash != hash)
            {
                await SetInvalidLogin(user);
                throw new Exception("Invalid user or password or attempt to login too fast after failed attempt");
            }
            return true;
        }
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> DropAllData()
        {
            int ret = 0;
            var list = await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}");
            foreach (var item in list)
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}", item);
                ret++;
            }
            return ret;
        }
    }
}
