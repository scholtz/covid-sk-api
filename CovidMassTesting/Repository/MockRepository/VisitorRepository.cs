using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
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
        private readonly SortedSet<string> docqueue = new SortedSet<string>();
        private readonly ILogger<VisitorRepository> logger;
        public VisitorRepository(
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            ISlotRepository slotRepository,
            IUserRepository userRepository
            ) : base(
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.VisitorRepository>(),
                redisCacheClient,
                emailSender,
                smsSender,
                placeRepository,
                slotRepository,
                userRepository
                )
        {
            logger = loggerFactory.CreateLogger<VisitorRepository>();
        }
        public override async Task<Visitor> Set(Visitor visitor, bool mustBeNew)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            if (mustBeNew && data.ContainsKey(visitor.Id)) throw new Exception("Item already exists");
            data[visitor.Id] = visitor;
            logger.LogInformation($"Visitor.Set {visitor.Id}");
            return visitor;
        }
        public async Task<bool> RemoveFromDocQueueAndSetTestStateAsTaken(string testId)
        {
            docqueue.Remove(testId);
            logger.LogInformation($"Visitor.RemoveFromDocQueue {testId}");
            return true;
        }
        public override async Task<bool> RemoveFromDocQueue(string testId)
        {
            docqueue.Remove(testId);
            logger.LogInformation($"Visitor.RemoveFromDocQueue {testId}");
            return true;
        }
        public override async Task<bool> AddToDocQueue(string testId)
        {
            docqueue.Add(testId);
            logger.LogInformation($"Visitor.AddToDocQueue {testId}");
            return true;
        }
        public override async Task<string> GetFirstItemFromQueue()
        {
            logger.LogInformation($"Visitor.GetFirstItemFromQueue");
            return docqueue.FirstOrDefault();
        }
        public override async Task<Visitor> Get(int code)
        {
            if (!data.ContainsKey(code)) return null;

            logger.LogInformation($"Visitor.Get {code}");
            return data[code];
        }
        public override async Task<IEnumerable<string>> ListAllKeys()
        {
            logger.LogInformation($"Visitor.ListAllKeys");
            return data.Keys.Select(k => k.ToString());
        }
        public override async Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {
            if (!pname2code.ContainsKey(personalNumber)) return null;
            logger.LogInformation($"Visitor.GETVisitorCodeFromPersonalNumber {personalNumber}");
            return pname2code[personalNumber];
        }
        public override async Task<int?> GETVisitorCodeFromTesting(string testCodeClear)
        {
            if (!testing2code.ContainsKey(testCodeClear)) return null;
            logger.LogInformation($"Visitor.GETVisitorCodeFromTesting {testCodeClear}");
            return testing2code[testCodeClear];
        }
        public override async Task MapPersonalNumberToVisitorCode(string personalNumber, int visitorCode)
        {
            logger.LogInformation($"Visitor.MapPersonalNumberToVisitorCode {personalNumber} {visitorCode}");
            pname2code[personalNumber] = visitorCode;
        }
        public override async Task MapTestingSetToVisitorCode(int codeInt, string testCodeClear)
        {
            logger.LogInformation($"Visitor.MapTestingSetToVisitorCode {codeInt} {testCodeClear}");
            testing2code[testCodeClear] = codeInt;
        }
        public override async Task UnMapPersonalNumber(string personalNumber)
        {
            logger.LogInformation($"Visitor.UnMapPersonalNumber {personalNumber}");
            if (pname2code.ContainsKey(personalNumber)) pname2code.TryRemove(personalNumber, out var _);
        }
        public override async Task UnMapTestingSet(string testCodeClear)
        {
            logger.LogInformation($"Visitor.UnMapTestingSet {testCodeClear}");
            if (testing2code.ContainsKey(testCodeClear)) testing2code.TryRemove(testCodeClear, out var _);
        }
        public override async Task<bool> Remove(int id)
        {
            logger.LogInformation($"Visitor.Remove {id}");
            if (!data.ContainsKey(id)) return false;
            data.TryRemove(id, out var _);
            return true;
        }
    }
}
