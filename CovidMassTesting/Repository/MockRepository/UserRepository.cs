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
    public class UserRepository : Repository.RedisRepository.UserRepository
    {
        private readonly ConcurrentDictionary<string, User> data = new ConcurrentDictionary<string, User>();

        public UserRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.UserRepository>(), redisCacheClient, emailSender)
        {
        }
        public override async Task<bool> Set(User user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            data[user.Email] = user;
            return true;
        }
        public override async Task<User> Get(string email)
        {
            if (!data.ContainsKey(email)) return null;
            return data[email];
        }
        public override async Task<IEnumerable<User>> ListAll()
        {
            return data.Values;
        }
    }
}
