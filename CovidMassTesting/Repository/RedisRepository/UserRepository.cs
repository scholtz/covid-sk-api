using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Model.SMS;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
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
        private readonly IStringLocalizer<UserRepository> localizer;
        private readonly ILogger<UserRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IEmailSender emailSender;
        private readonly ISMSSender smsSender;
        private readonly IConfiguration configuration;
        private readonly IPlaceRepository placeRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly string REDIS_KEY_USERS_OBJECTS = "USERS";
        private readonly string REDIS_KEY_INVITATION_OBJECTS = "INVITATIONS";

        private readonly int RehashN = 99;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        /// <param name="placeRepository"></param>
        /// <param name="placeProviderRepository"></param>
        public UserRepository(
            IStringLocalizer<UserRepository> localizer,
            IConfiguration configuration,
            ILogger<UserRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            IPlaceProviderRepository placeProviderRepository
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.emailSender = emailSender;
            this.smsSender = smsSender;
            this.configuration = configuration;
            this.placeRepository = placeRepository;
            this.placeProviderRepository = placeProviderRepository;
        }

        /// <summary>
        /// Inserts new user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="inviterName"></param>
        /// <param name="companyName"></param>
        /// <returns></returns>
        public async Task<bool> Add(User user, string inviterName, string companyName)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrEmpty(inviterName))
            {
                throw new ArgumentNullException(nameof(inviterName));
            }

            (string pass, string hash, string cohash) = GeneratePassword();
            user.PswHash = hash;
            user.CoHash = cohash;
            var ret = await SetUser(user, true);

            await emailSender.SendEmail(
                localizer[Repository_RedisRepository_UserRepository.Invitation_to_covid_testing_place],
                user.Email,
                user.Name,
                new Model.Email.InvitationEmail(CultureInfo.CurrentCulture.Name, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                {
                    Name = user.Name,
                    Password = pass,
                    Roles = user.Roles?.ToArray(),
                    InviterName = inviterName,
                    CompanyName = companyName,
                    WebPath = configuration["FrontedURL"]
                });
            if (!string.IsNullOrEmpty(user.Phone))
            {
                await smsSender.SendSMS(user.Phone, new Message(string.Format(localizer[Repository_RedisRepository_UserRepository.Dear__0___we_have_registered_you_into_mass_covid_testing_system__Please_check_your_email_].Value, user.Name)));
            }
            return ret;
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

            using RandomGenerator rand = new RandomGenerator();
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
        public virtual async Task<bool> SetUser(User user, bool mustBeNew)
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
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Error_creating_record_in_the_database].Value);
            }
            return true;
        }
        /// <summary>
        /// Set user. Encodes and stores to db.
        /// </summary>
        /// <param name="invitation"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public virtual async Task<Invitation> SetInvitation(Invitation invitation, bool mustBeNew)
        {
            if (invitation is null)
            {
                throw new ArgumentNullException(nameof(invitation));
            }

            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_INVITATION_OBJECTS}", invitation.InvitationId, invitation, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Error_creating_record_in_the_database].Value);
            }
            return invitation;
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
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public virtual async Task<User> GetUser(string email, string placeProviderId)
        {
            logger.LogInformation($"User loaded from database: {email}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}", email);
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            User ret = Newtonsoft.Json.JsonConvert.DeserializeObject<User>(decoded);
            // enhance user object for place provider groups
            if (!string.IsNullOrEmpty(placeProviderId))
            {
                var groups = await placeProviderRepository.GetUserGroups(email, placeProviderId);
                if (ret.Roles == null) ret.Roles = new List<string>();
                foreach (var group in groups)
                {
                    if (!ret.Roles.Contains(group)) ret.Roles.Add(group);
                }
            }
            return ret;
        }
        /// <summary>
        /// GetInvitation
        /// </summary>
        /// <param name="invitationId"></param>
        /// <returns></returns>
        public virtual Task<Invitation> GetInvitation(string invitationId)
        {
            return redisCacheClient.Db0.HashGetAsync<Invitation>($"{configuration["db-prefix"]}{REDIS_KEY_INVITATION_OBJECTS}", invitationId);
        }

        /// <summary>
        /// Lists all users 
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<User>> ListAll(string placeProviderId)
        {
            var ret = new List<User>();
            var list = await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_USERS_OBJECTS}");
            foreach (var item in list)
            {
                try
                {
                    ret.Add(await GetUser(item, placeProviderId));
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Unable to deserialize user: {item}");
                }
            }
            return ret;
        }
        /// <summary>
        /// Lists invitations by pp
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Invitation>> ListInvitationsByPP(string placeProviderId)
        {
            if (string.IsNullOrEmpty(placeProviderId))
            {
                throw new ArgumentNullException(nameof(placeProviderId));
            }

            var ret = new List<Invitation>();
            var list = await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_INVITATION_OBJECTS}");
            foreach (var item in list)
            {
                try
                {
                    ret.Add(await GetInvitation(item));
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Unable to deserialize user: {item}");
                }
            }
            return ret.Where(i => i.PlaceProviderId == placeProviderId).OrderByDescending(i => i.LastUpdate);
        }
        /// <summary>
        /// Lists invitations by email
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<Invitation>> ListInvitationsByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email));
            }

            var ret = new List<Invitation>();
            var list = await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_INVITATION_OBJECTS}");
            foreach (var item in list)
            {
                try
                {
                    ret.Add(await GetInvitation(item));
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, $"Unable to deserialize user: {item}");
                }
            }
            return ret.Where(i => i.Email == email).OrderByDescending(i => i.LastUpdate);
        }
        /// <summary>
        /// Registration manager can set his place at which he performs tests
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeId"></param>
        /// <param name="placeProviderId">PP id for user token pp scope groups</param>
        /// <returns></returns>
        public async Task<bool> SetLocation(string email, string placeId, string placeProviderId)
        {
            if (string.IsNullOrEmpty(placeId))
            {
                var user = await GetUser(email, placeProviderId);
                user.Place = null;
                user.PlaceLastCheck = null;
                return await SetUser(user, false);
            }
            else
            {
                var place = await placeRepository.GetPlace(placeId);
                if (place == null) throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_place_provided].Value);
                var user = await GetUser(email, placeProviderId);
                user.Place = placeId;
                user.PlaceLastCheck = DateTimeOffset.UtcNow;
                return await SetUser(user, false);
            }
        }

        /// <summary>
        /// Create admin users from the configuration
        /// </summary>
        /// <returns></returns>
        public async Task CreateAdminUsersFromConfiguration()
        {
            var users = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
            if (users == null)
            {
                logger.LogInformation("No admin users are defined in the configuration");
                return;
            }
            foreach (var usr in users)
            {
                try
                {
                    var user = await GetUser(usr.Email, null);
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
                            }, "System", "Global admin");
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
                            await SetUser(newUser, true);
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
                            await SetUser(newUser, false);
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
            var user = await GetUser(email, null);
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
            await SetUser(user, false);
            return user;
        }
        private async Task<User> SetInvalidLogin(User user)
        {
            user.InvalidLogin = DateTimeOffset.UtcNow;
            await SetUser(user, false);
            return user;
        }
        /// <summary>
        /// Returns JWT token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public async Task<string> Authenticate(string email, string hash)
        {
            var isGlobalAdmin = InAnyGroup(email, new string[] { Groups.Admin }, null).Result;

            var places = await placeProviderRepository.ListPrivate(email, isGlobalAdmin);
            var placeProviderId = places?.FirstOrDefault()?.PlaceProviderId;
            var usr = await GetUser(email, placeProviderId);
            if (usr == null)
            {
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_user_or_password].Value);
            }
            if (usr.InvalidLogin.HasValue && usr.InvalidLogin.Value.AddSeconds(1) > DateTimeOffset.UtcNow)
            {
                await SetInvalidLogin(usr);
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_user_or_password].Value);
            }
            var ourHash = Encoding.ASCII.GetBytes($"{usr.PswHash}{usr.CoData}").GetSHA256Hash();
            if (ourHash != hash)
            {
                await SetInvalidLogin(usr);
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_user_or_password].Value);
            }
            return Token.CreateToken(usr, configuration, placeProviderId);
        }

        /// <summary>
        /// Change password
        /// 
        /// If successful, returns new JWT token
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="oldHash">Old password</param>
        /// <param name="newHash">New password</param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public async Task<string> ChangePassword(string email, string oldHash, string newHash, string placeProviderId)
        {
            var user = await GetUser(email, placeProviderId);
            if (user == null) throw new Exception(localizer[Repository_RedisRepository_UserRepository.User_not_found_by_email].Value);
            if (user.PswHash != oldHash) throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_old_password].Value);
            user.PswHash = newHash;
            user.CoHash = user.CoData;
            if (await SetUser(user, false))
            {
                var isGlobalAdmin = InAnyGroup(email, new string[] { Groups.Admin }, null).Result;
                var places = await placeProviderRepository.ListPrivate(user.Email, isGlobalAdmin);
                return Token.CreateToken(user, configuration, places?.FirstOrDefault()?.PlaceProviderId);
            }
            return "";
        }
        /// <summary>
        /// Change password
        /// 
        /// If successful, returns new JWT token
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public async Task<string> SetPlaceProvider(string email, string placeProviderId)
        {
            var user = await GetUser(email, placeProviderId);
            if (user == null) throw new Exception(localizer[Repository_RedisRepository_UserRepository.User_not_found_by_email].Value);
            return Token.CreateToken(user, configuration, placeProviderId);
        }
        /// <summary>
        /// Checks if user with specified email has any of reqested groups
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="role">Search any of this roles</param>
        /// <returns></returns>
        public async Task<bool> InAnyGroup(string email, string[] role, string placeProviderId)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException(localizer[Repository_RedisRepository_UserRepository.Email_must_not_be_empty].Value);
            }

            if (role is null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            if (role.Length == 0) return true;


            var usr = await GetUser(email, placeProviderId);
            if (usr != null && usr.Roles != null)
            {
                foreach (var dbrole in usr.Roles?.ToArray())
                {
                    if (dbrole.Contains(","))
                    {
                        foreach (var addrole in dbrole.Split(','))
                        {
                            if (!usr.Roles.Contains(addrole))
                            {
                                usr.Roles.Add(addrole);
                            }
                        }
                    }
                }

                foreach (var lookupRole in role)
                {
                    if (usr.Roles.Contains(lookupRole)) return true;
                }
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
            return (await GetUser(email, null)).ToPublic();
        }


        /// <summary>
        /// Administrator is authorized to delete all data in the database
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public async Task<bool> DropDatabaseAuthorize(string email, string hash)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException(localizer[Repository_RedisRepository_UserRepository.Email_must_not_be_empty].Value, nameof(email));
            }

            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentException(localizer[Repository_RedisRepository_UserRepository.Hash_must_not_be_empty].Value, nameof(hash));
            }
            var user = await GetUser(email, null);
            if (user == null) throw new Exception(localizer[Repository_RedisRepository_UserRepository.User_not_found_by_email].Value);
            if (user.InvalidLogin.HasValue && user.InvalidLogin.Value.AddSeconds(1) > DateTimeOffset.UtcNow)
            {
                await SetInvalidLogin(user);
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_user_or_password_or_attempt_to_login_too_fast_after_failed_attempt].Value);
            }
            var ourHash = Encoding.ASCII.GetBytes($"{user.PswHash}{user.CoData}").GetSHA256Hash();
            if (ourHash != hash)
            {
                await SetInvalidLogin(user);
                throw new Exception(localizer[Repository_RedisRepository_UserRepository.Invalid_user_or_password_or_attempt_to_login_too_fast_after_failed_attempt].Value);
            }
            return true;
        }
        /// <summary>
        /// invites person
        /// </summary>
        /// <param name="invitation"></param>
        /// <returns></returns>
        public async Task<Invitation> Invite(Invitation invitation)
        {
            invitation.InvitationId = Guid.NewGuid().ToString();
            var ret = await SetInvitation(invitation, true);
            var user = await GetUser(invitation.Email, invitation.PlaceProviderId);
            var pp = await placeProviderRepository.GetPlaceProvider(invitation.PlaceProviderId);
            if (pp == null) throw new Exception("Place provider has not been found");
            invitation.CompanyName = pp.CompanyName;
            if (user == null)
            {
                // invitation with new user
                await Add(
                    new User()
                    {
                        Email = invitation.Email,
                        Phone = invitation.Phone,
                        Name = invitation.Name,
                    },
                    invitation.InviterName,
                    pp.CompanyName
                );
            }
            else
            {
                // invitation of existing user

                await emailSender.SendEmail(
                    localizer[Repository_RedisRepository_UserRepository.Invitation_to_covid_testing_place],
                    user.Email,
                    user.Name,
                    new Model.Email.InvitationEmail(CultureInfo.CurrentCulture.Name, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                    {
                        Name = user.Name,
                        Roles = user.Roles?.ToArray(),
                        InviterName = invitation.InviterName,
                        CompanyName = pp.CompanyName,
                        WebPath = configuration["FrontedURL"]
                    });
            }

            return ret;
        }
        /// <summary>
        /// Accept/Deny the invitation. Stores accepted invitation to pp.
        /// </summary>
        /// <param name="invitationId"></param>
        /// <param name="accepted"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public async Task<Invitation> ProcessInvitation(string invitationId, bool accepted, string userEmail)
        {
            var invitation = await GetInvitation(invitationId);
            if (invitation == null) throw new Exception("Invitation not found");

            var pp = await placeProviderRepository.GetPlaceProvider(invitation.PlaceProviderId);
            if (pp == null) throw new Exception("Invitation place provider has not been found");

            if (invitation.Status != InvitationStatus.Invited) throw new Exception("Invitation has been already processed");
            if (invitation.Email != userEmail) throw new Exception("Invitation has been sent to someone else");
            invitation.CompanyName = pp.CompanyName;
            invitation.StatusTime = DateTimeOffset.Now;
            if (accepted)
            {
                invitation.Status = InvitationStatus.Accepted;
            }
            else
            {
                invitation.Status = InvitationStatus.Declined;
            }
            invitation = await SetInvitation(invitation, false);
            if (accepted)
            {
                if (pp.Users == null) pp.Users = new List<Invitation>();
                if (!pp.Users.Any(p => p.Email == userEmail))
                {
                    pp.Users.Add(invitation);
                    await placeProviderRepository.SetPlaceProvider(pp);
                }
            }

            return invitation;
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
            list = await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_INVITATION_OBJECTS}");
            foreach (var item in list)
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_INVITATION_OBJECTS}", item);
                ret++;
            }
            return ret;
        }
    }
}
