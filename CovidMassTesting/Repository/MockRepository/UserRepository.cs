using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.MockRepository
{
    /// <summary>
    /// In memory user repository
    /// </summary>
    public class UserRepository : Repository.RedisRepository.UserRepository
    {
        private readonly ConcurrentDictionary<string, User> data = new ConcurrentDictionary<string, User>();
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        public UserRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.UserRepository>(), redisCacheClient, emailSender)
        {
        }
        /// <summary>
        /// set user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public override async Task<bool> Set(User user, bool mustBeNew)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (mustBeNew)
            {
                if (data.ContainsKey(user.Email))
                {
                    throw new Exception("User already exists");
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
        public override async Task<User> Get(string email)
        {
            if (!data.ContainsKey(email)) return null;
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
    }
}
