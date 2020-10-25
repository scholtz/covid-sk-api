using CovidMassTesting.Model;
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
        private ConcurrentDictionary<string, User> data = new ConcurrentDictionary<string, User>();

        public UserRepository(
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(loggerFactory.CreateLogger<Repository.RedisRepository.UserRepository>(), redisCacheClient)
        {
            Add(new User()
            {
                Email = "ludovit@scholtz.sk",
                Name = "Ludovit Scholtz"
            }).Wait();
        }
        public override async Task<bool> Add(User user)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            data[user.Email] = user;
            return true;
        }
        public override async Task<IEnumerable<User>> ListAll()
        {
            return data.Values;
        }
    }
}
