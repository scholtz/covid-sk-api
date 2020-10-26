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
        public async Task<Visitor> Add(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor.Id = await CreateNewVisitorId();
            return await Set(visitor);
        }
        public virtual async Task<Visitor> Get(int code)
        {
            logger.LogInformation($"Visitor loaded from database: {code.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>(REDIS_KEY_VISITORS_OBJECTS, code.ToString());
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(decoded);
        }
        public virtual async Task<Visitor> Set(Visitor visitor)
        {
            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            logger.LogInformation($"Setting object {visitor.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (!await redisCacheClient.Db0.HashSetAsync(REDIS_KEY_VISITORS_OBJECTS, visitor.Id.ToString(CultureInfo.InvariantCulture), encoded, true))
            {
                throw new Exception("Error creating record in the database");
            }
            return visitor;
        }
        public async Task<bool> UpdateTestingState(int code, string state)
        {
            logger.LogInformation($"Updating state for {code.GetHashCode()}");
            var item = await Get(code);
            item.Result = state;
            await Set(item);
            return true;
        }
        public async Task<Result> GetTest(int code, string pass)
        {
            if (string.IsNullOrEmpty(pass))
            {
                throw new ArgumentException($"'{nameof(pass)}' cannot be null or empty", nameof(pass));
            }
            if (pass.Length < 4)
            {
                throw new Exception("Invalid code");
            }
            var visitor = await Get(code);
            if (visitor.RC?.Length > 4 && !visitor.RC.Trim().EndsWith(pass.Trim()))
            {
                throw new Exception("Invalid code");
            }
            if (visitor.Passport?.Length > 4 && !visitor.Passport.Trim().EndsWith(pass.Trim()))
            {
                throw new Exception("Invalid code");
            }
            return new Result { State = visitor.Result };
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
