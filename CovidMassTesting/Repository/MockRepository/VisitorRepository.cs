using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
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
    /// Visitor mock repository
    /// </summary>
    public class VisitorRepository : Repository.RedisRepository.VisitorRepository
    {
        private readonly ConcurrentDictionary<int, Visitor> data = new ConcurrentDictionary<int, Visitor>();
        private readonly ConcurrentDictionary<string, string> verification = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, int> testing2code = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> pname2code = new ConcurrentDictionary<string, int>();
        private readonly SortedSet<string> docqueue = new SortedSet<string>();
        private readonly IConfiguration configuration;
        private readonly ILogger<VisitorRepository> logger;
        private int TestInt { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        /// <param name="placeRepository"></param>
        /// <param name="slotRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="userRepository"></param>
        public VisitorRepository(
            IStringLocalizer<Repository.RedisRepository.VisitorRepository> localizer,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            ISlotRepository slotRepository,
            IPlaceProviderRepository placeProviderRepository,
            IUserRepository userRepository
            ) : base(
                localizer,
                configuration,
                loggerFactory.CreateLogger<Repository.RedisRepository.VisitorRepository>(),
                redisCacheClient,
                emailSender,
                smsSender,
                placeRepository,
                slotRepository,
                placeProviderRepository,
                userRepository
                )
        {
            logger = loggerFactory.CreateLogger<VisitorRepository>();
            this.configuration = configuration;
        }
        /// <summary>
        /// Set visitor
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public override async Task<Visitor> SetVisitor(Visitor visitor, bool mustBeNew)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor = await FixVisitor(visitor, false);

            if (mustBeNew && data.ContainsKey(visitor.Id)) throw new Exception("Item already exists");
            data[visitor.Id] = visitor;
            logger.LogInformation($"Visitor.Set {visitor.Id}");
            return visitor;
        }
        /// <summary>
        /// remove from doc queue
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        public override async Task<bool> RemoveFromDocQueue(string testId)
        {
            docqueue.Remove(testId);
            logger.LogInformation($"Visitor.RemoveFromDocQueue {testId}");
            return true;
        }
        /// <summary>
        /// add to doc queue
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        public override async Task<bool> AddToDocQueue(string testId)
        {
            docqueue.Add(testId);
            logger.LogInformation($"Visitor.AddToDocQueue {testId}");
            return true;
        }
        /// <summary>
        /// Get first item in doc queue
        /// </summary>
        /// <returns></returns>
        public override async Task<string> GetFirstItemFromQueue()
        {
            logger.LogInformation($"Visitor.GetFirstItemFromQueue");
            return docqueue.FirstOrDefault();
        }
        /// <summary>
        /// loads visitor by code
        /// </summary>
        /// <param name="codeInt"></param>
        /// <returns></returns>
        public override async Task<Visitor> GetVisitor(int codeInt, bool fixOnLoad = true)
        {
            if (!data.ContainsKey(codeInt)) return null;

            logger.LogInformation($"Visitor.Get {codeInt}");
            if (fixOnLoad)
            {
                return await FixVisitor(data[codeInt], true);
            }
            else
            {
                return data[codeInt];
            }
        }
        /// <summary>
        /// List all keys
        /// </summary>
        /// <returns></returns>
        public override async Task<IEnumerable<string>> ListAllKeys()
        {
            logger.LogInformation($"Visitor.ListAllKeys");
            return data.Keys.Select(k => k.ToString());
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public override async Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {
            if (!pname2code.ContainsKey(personalNumber)) return null;
            logger.LogInformation($"Visitor.GETVisitorCodeFromPersonalNumber {personalNumber}");
            return pname2code[personalNumber];
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public override async Task<int?> GETVisitorCodeFromTesting(string testCodeClear)
        {
            if (!testing2code.ContainsKey(testCodeClear)) return null;
            logger.LogInformation($"Visitor.GETVisitorCodeFromTesting {testCodeClear}");
            return testing2code[testCodeClear];
        }
        /// <summary>
        /// map personal number to visitor code
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <param name="visitorCode"></param>
        /// <returns></returns>
        public override async Task MapPersonalNumberToVisitorCode(string personalNumber, int visitorCode)
        {
            logger.LogInformation($"Visitor.MapPersonalNumberToVisitorCode {personalNumber} {visitorCode}");
            pname2code[personalNumber] = visitorCode;
        }
        /// <summary>
        /// Map testing set to visitor code
        /// </summary>
        /// <param name="codeInt"></param>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public override async Task MapTestingSetToVisitorCode(int codeInt, string testCodeClear)
        {
            logger.LogInformation($"Visitor.MapTestingSetToVisitorCode {codeInt} {testCodeClear}");
            testing2code[testCodeClear] = codeInt;
        }
        /// <summary>
        /// Unmap personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public override async Task UnMapPersonalNumber(string personalNumber)
        {
            logger.LogInformation($"Visitor.UnMapPersonalNumber {personalNumber}");
            if (pname2code.ContainsKey(personalNumber)) pname2code.TryRemove(personalNumber, out var _);
        }
        /// <summary>
        /// Unmap testing set
        /// </summary>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public override async Task UnMapTestingSet(string testCodeClear)
        {
            logger.LogInformation($"Visitor.UnMapTestingSet {testCodeClear}");
            if (testing2code.ContainsKey(testCodeClear)) testing2code.TryRemove(testCodeClear, out var _);
        }
        /// <summary>
        /// Removes id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<bool> Remove(int id)
        {
            logger.LogInformation($"Visitor.Remove {id}");
            if (!data.ContainsKey(id)) return false;
            data.TryRemove(id, out var _);
            return true;
        }
        /// <summary>
        /// delete all data
        /// </summary>
        /// <returns></returns>
        public override async Task<int> DropAllData()
        {
            var ret = data.Count + testing2code.Count + pname2code.Count + docqueue.Count;
            data.Clear();
            testing2code.Clear();
            pname2code.Clear();
            docqueue.Clear();
            return ret;
        }

        /// <summary>
        /// Tests the storage
        /// </summary>
        /// <returns></returns>
        public override async Task<int> TestStorage()
        {
            using var rand = new RandomGenerator();
            var toSave = rand.Next(100000, 900000);
            this.TestInt = toSave;
            if (toSave != TestInt) throw new Exception("Storage does not work");
            return toSave;
        }



        /// <summary>
        /// Decode visitor data from database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<VerificationData> GetResult(string id)
        {
            logger.LogInformation($"VerificationData loaded from database: {id.GetHashCode()}");
            var encoded = verification[id];
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<VerificationData>(decoded);
        }
        /// <summary>
        /// Encode visitor data and store to database
        /// </summary>
        /// <param name="verificationData"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public override async Task<VerificationData> SetResult(Model.VerificationData verificationData, bool mustBeNew)
        {
            if (verificationData is null)
            {
                throw new ArgumentNullException(nameof(verificationData));
            }

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(verificationData);
            logger.LogInformation($"Setting verificationData {verificationData.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (mustBeNew && verification.ContainsKey(verificationData.Id)) throw new Exception("Must be new");
            verification[verificationData.Id] = encoded;
            return verificationData;
        }
    }
}
