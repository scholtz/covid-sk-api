using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
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
        private readonly ConcurrentDictionary<string, int> testing2code = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> pname2code = new ConcurrentDictionary<string, int>();

        public VisitorRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            IPlaceRepository placeRepository
            ) : base(
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.VisitorRepository>(),
                redisCacheClient,
                emailSender,
                placeRepository
                )
        {

        }
        public override async Task<Visitor> Set(Visitor visitor, bool mustBeNew)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            if (mustBeNew && data.ContainsKey(visitor.Id)) throw new Exception("Item already exists");
            data[visitor.Id] = visitor;
            return visitor;
        }
        public override async Task<Visitor> Get(int code)
        {
            if (!data.ContainsKey(code)) return null;

            return data[code];
        }
        public override async Task<IEnumerable<string>> ListAllKeys()
        {
            return data.Keys.Select(k => k.ToString());
        }
        public override async Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {
            if (!pname2code.ContainsKey(personalNumber)) return null;
            return pname2code[personalNumber];
        }
        public override async Task<int?> GETVisitorCodeFromTesting(string testCodeClear)
        {
            if (!testing2code.ContainsKey(testCodeClear)) return null;
            return testing2code[testCodeClear];
        }
        public override async Task MapPersonalNumberToVisitorCode(string personalNumber, int visitorCode)
        {
            pname2code[personalNumber] = visitorCode;
        }
        public override async Task MapTestingSetToVisitorCode(int codeInt, string testCodeClear)
        {
            testing2code[testCodeClear] = codeInt;
        }
    }
}
