using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.MockRepository
{
    /// <summary>
    /// In memory user repository
    /// </summary>
    public class UserRepository : Repository.RedisRepository.UserRepository
    {
        private readonly IStringLocalizer<UserRepository> localizer;
        private readonly ConcurrentDictionary<string, User> data = new ConcurrentDictionary<string, User>();
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="localizer2"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        /// <param name="placeRepository"></param>
        /// <param name="placeProviderRepository"></param>
        public UserRepository(
            IStringLocalizer<UserRepository> localizer,
            IStringLocalizer<Repository.RedisRepository.UserRepository> localizer2,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            IPlaceProviderRepository placeProviderRepository
        ) : base(
                localizer2,
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.UserRepository>(),
                redisCacheClient,
                emailSender,
                smsSender,
                placeRepository,
                placeProviderRepository
                )
        {
            this.localizer = localizer;
        }
        /// <summary>
        /// set user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public override async Task<bool> SetUser(User user, bool mustBeNew)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (mustBeNew)
            {
                if (data.ContainsKey(user.Email))
                {
                    throw new Exception(localizer["User already exists"].Value);
                }
            }

            data[user.Email] = user;
            return true;
        }
        /// <summary>
        /// Gets user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override async Task<User> GetUser(string email)
        {
            if (!data.ContainsKey(email))
            {
                return null;
            }

            return data[email];
        }
        /// <summary>
        /// Returns all users
        /// </summary>
        /// <returns></returns>
        public override async Task<IEnumerable<User>> ListAll()
        {
            return data.Values;
        }
        /// <summary>
        /// Removes user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override async Task<bool> Remove(string email)
        {
            data.TryRemove(email, out var _);
            return true;
        }
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = data.Count;
            data.Clear();
            return ret;
        }
    }
}
