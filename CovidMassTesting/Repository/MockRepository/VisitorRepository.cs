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
    public class VisitorRepository : Repository.RedisRepository.VisitorRepository
    {
        private readonly ConcurrentDictionary<int, Visitor> data = new ConcurrentDictionary<int, Visitor>();

        public VisitorRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender
            ) : base(configuration, loggerFactory.CreateLogger<Repository.RedisRepository.VisitorRepository>(), redisCacheClient, emailSender)
        {

        }
        public override async Task<Visitor> Set(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            data[visitor.Id] = visitor;
            return visitor;
        }
        public override async Task<Visitor> Get(int code)
        {
            return data[code];
        }
        public override async Task<IEnumerable<string>> ListAllKeys()
        {
            return data.Keys.Select(k => k.ToString());
        }
    }
}
