using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    public class UserRepository : IUserRepository
    {
        private readonly ILogger<UserRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly string REDIS_KEY_USERS_OBJECTS = "PLACE";
        private readonly string REDIS_KEY_USERS_LIST = "PLACESLIST";

        public UserRepository(
            ILogger<UserRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
        }
        public virtual async Task<bool> Add(User place)
        {
            try
            {
                if (!await redisCacheClient.Db0.HashSetAsync(REDIS_KEY_USERS_OBJECTS, place.Email.ToString(), place, true))
                {
                    throw new Exception("Error creating place");
                }
                await redisCacheClient.Db0.SetAddAsync($"{REDIS_KEY_USERS_LIST}_{place.Email}", $"{place.Email}");
                return true;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return false;
            }
        }

        public virtual Task<IEnumerable<User>> ListAll()
        {
            return redisCacheClient.Db0.HashValuesAsync<User>(REDIS_KEY_USERS_OBJECTS);
        }
    }
}
