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
    public class VisitorRepository : Repository.RedisRepository.VisitorRepository
    {
        private ConcurrentDictionary<int, Visitor> data = new ConcurrentDictionary<int, Visitor>();

        public VisitorRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.VisitorRepository>(), redisCacheClient)
        {

        }
        public override async Task<Visitor> Add(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor.Id = await CreateNewVisitorId();

            data[visitor.Id] = visitor;
            return visitor;
        }
        public override async Task<IEnumerable<string>> ListAllKeys()
        {
            return data.Keys.Select(k => k.ToString());
        }
    }
}
