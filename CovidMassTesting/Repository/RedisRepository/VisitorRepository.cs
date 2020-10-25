using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    public class VisitorRepository : IVisitorRepository
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<VisitorRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly string REDIS_KEY_VISITORS_OBJECTS = "VISITOR";

        public VisitorRepository(
            IConfiguration configuration,
            ILogger<VisitorRepository> logger,
            IRedisCacheClient redisCacheClient
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
        }
        public virtual async Task<Visitor> Add(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.Id = await CreateNewVisitorId();

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            logger.LogInformation($"Creating object {visitor.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (!await redisCacheClient.Db0.HashSetAsync(REDIS_KEY_VISITORS_OBJECTS, visitor.Id.ToString(CultureInfo.InvariantCulture), encoded, true))
            {
                throw new Exception("Error creating record in the database");
            }
            return visitor;
        }

        protected async Task<int> CreateNewVisitorId()
        {
            var existingIds = await ListAllKeys();
            var rand = new Random();
            var next = rand.Next(100000000, 999999999);
            while (existingIds.Contains(next.ToString(CultureInfo.InvariantCulture)))
            {
                next = rand.Next(100000000, 999999999);
            }
            return next;
        }

        public virtual Task<IEnumerable<string>> ListAllKeys()
        {
            return redisCacheClient.Db0.HashKeysAsync(REDIS_KEY_VISITORS_OBJECTS);
        }
    }
}
