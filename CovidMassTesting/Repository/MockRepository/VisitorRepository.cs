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
        private readonly ConcurrentDictionary<string, Result> dataResults = new ConcurrentDictionary<string, Result>();
        private readonly ConcurrentDictionary<string, string> verification = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, int> testing2code = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, string> testing2lastresult = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, int> pname2code = new ConcurrentDictionary<string, int>();

        private readonly ConcurrentDictionary<string, string> registrations = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, string> id2registration = new ConcurrentDictionary<string, string>();


        private readonly ConcurrentDictionary<long, ConcurrentDictionary<int, int>> day2visitor = new ConcurrentDictionary<long, ConcurrentDictionary<int, int>>();
        private readonly ConcurrentDictionary<long, long> days = new ConcurrentDictionary<long, long>();

        private readonly SortedSet<string> docqueue = new SortedSet<string>();
        private readonly Queue<string> resultqueue = new Queue<string>();
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

            if (mustBeNew && data.ContainsKey(visitor.Id))
            {
                throw new Exception("Item already exists");
            }

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
            if (!data.ContainsKey(codeInt))
            {
                return null;
            }

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
        public override async Task<IEnumerable<string>> ListAllKeys(DateTimeOffset? day = null)
        {
            logger.LogInformation($"Visitor.ListAllKeys");
            if (day.HasValue)
            {
                return day2visitor[day.Value.Ticks].Values.Select(v => v.ToString());
            }
            return data.Keys.Select(k => k.ToString());
        }
        /// <summary>
        /// Get
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public override async Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {
            if (!pname2code.ContainsKey(personalNumber))
            {
                personalNumber = FormatDocument(personalNumber);
            }

            if (!pname2code.ContainsKey(personalNumber))
            {
                return null;
            }

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
            if (!testing2code.ContainsKey(testCodeClear))
            {
                return null;
            }

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
            personalNumber = FormatDocument(personalNumber);
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
            personalNumber = FormatDocument(personalNumber);
            logger.LogInformation($"Visitor.UnMapPersonalNumber {personalNumber}");
            if (pname2code.ContainsKey(personalNumber))
            {
                pname2code.TryRemove(personalNumber, out var _);
            }
        }
        /// <summary>
        /// Unmap testing set
        /// </summary>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public override async Task UnMapTestingSet(string testCodeClear)
        {
            logger.LogInformation($"Visitor.UnMapTestingSet {testCodeClear}");
            if (testing2code.ContainsKey(testCodeClear))
            {
                testing2code.TryRemove(testCodeClear, out var _);
            }
        }
        /// <summary>
        /// Removes id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<bool> Remove(int id)
        {
            logger.LogInformation($"Visitor.Remove {id}");
            if (!data.ContainsKey(id))
            {
                return false;
            }

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
            TestInt = toSave;
            if (toSave != TestInt)
            {
                throw new Exception("Storage does not work");
            }

            return toSave;
        }



        /// <summary>
        /// Decode visitor data from database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task<VerificationData> GetResultVerification(string id)
        {
            logger.LogInformation($"VerificationData loaded from database: {id.GetHashCode()}");
            var encoded = verification[id];
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

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
            if (mustBeNew && verification.ContainsKey(verificationData.Id))
            {
                throw new Exception("Must be new");
            }

            verification[verificationData.Id] = encoded;

            return verificationData;
        }

        public override async Task<bool> AddToResultQueue(string resultId)
        {
            resultqueue.Enqueue(resultId);
            return true;
        }
        public override async Task<string> GetFirstItemFromResultQueue()
        {
            logger.LogInformation($"Visitor.GetFirstItemFromResultQueue");
            return resultqueue.FirstOrDefault();
        }
        public override async Task<Result> GetResultObject(string id)
        {
            return dataResults[id];
        }
        public override async Task<IEnumerable<string>> ListAllKeysResults()
        {
            return dataResults.Keys;
        }
        public override async Task<IEnumerable<string>> ListAllResultKeys()
        {
            return verification.Keys;
        }
        public override async Task<string> PopFromResultQueue()
        {
            resultqueue.TryDequeue(out var ret);
            logger.LogInformation($"Visitor.RemoveFromDocQueue {ret}");
            return ret;
        }
        public override async Task<bool> RemoveResult(string id)
        {
            dataResults.TryRemove(id, out var removed);
            return removed != null;
        }
        public override async Task<Result> SetResultObject(Result result, bool mustBeNew)
        {
            if (mustBeNew)
            {
                if (dataResults.ContainsKey(result.Id))
                {
                    throw new Exception("Result must be new");
                }
            }
            dataResults[result.Id] = result;
            testing2lastresult[result.TestingSetId] = result.Id;
            return result;
        }

        public override async Task<bool> MapDay(long day)
        {
            days[day] = day;
            return true;
        }
        public override async Task<bool> MapDayToVisitorCode(long day, int visitorCode)
        {
            if (!day2visitor.ContainsKey(day))
            {
                day2visitor[day] = new ConcurrentDictionary<int, int>();
            }

            day2visitor[day][visitorCode] = visitorCode;
            return true;
        }
        public override async Task<bool> UnMapDay(long day)
        {
            if (days.TryRemove(day, out var item))
            {
                return true;
            }
            return false;
        }
        public override async Task<bool> UnMapDayToVisitorCode(long day, int visitorCode)
        {
            if (!day2visitor.ContainsKey(day))
            {
                return false;
            }

            if (!day2visitor[day].ContainsKey(visitorCode))
            {
                return false;
            }

            if (day2visitor[day].TryRemove(visitorCode, out var item))
            {
                return true;
            }
            return false;

        }


        public override async Task<Registration> GetRegistration(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            logger.LogInformation($"Registration loaded from database: {(configuration["key"] + id).GetSHA256Hash()}");
            var encoded = registrations[id];
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(decoded);
        }
        public override async Task<bool> RemoveRegistration(string id)
        {
            if (registrations.TryRemove(id, out var _))
            {
                return true;
            }
            return false;
        }

        public override async Task<Registration> SetRegistration(Registration registration, bool mustBeNew)
        {
            if (registration is null)
            {
                throw new ArgumentNullException(nameof(registration));
            }
            registration.LastUpdate = DateTimeOffset.Now;

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(registration);
            logger.LogInformation($"Setting object {registration.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (mustBeNew && registrations.ContainsKey(registration.Id))
            {
                throw new Exception("Must be new");
            }

            registrations[registration.Id] = encoded;
            if (!string.IsNullOrEmpty(registration.RC))
            {
                await MapHashedIdToRegistration($"{configuration["key"]}-{registration.RC}".GetSHA256Hash(), registration.Id);
            }
            if (!string.IsNullOrEmpty(registration.Passport))
            {
                await MapHashedIdToRegistration($"{configuration["key"]}-{registration.Passport}".GetSHA256Hash(), registration.Id);
            }
            foreach (var item in registration.CompanyIdentifiers)
            {
                await MapHashedIdToRegistration(MakeCompanyPeronalNumberHash(item.CompanyId, item.EmployeeId), registration.Id);
            }
            return registration;
        }
        public override async Task<IEnumerable<string>> ListAllRegistrationKeys()
        {
            return registrations.Keys;
        }

        public override async Task MapHashedIdToRegistration(string hashedId, string registrationId)
        {
            id2registration[hashedId] = registrationId;
        }
        public override async Task UnMapHashedIdToRegistration(string hashedId)
        {
            id2registration.TryRemove(hashedId, out var _);
        }
        public override async Task<string> GetRegistrationIdFromHashedId(string hashedId)
        {
            if (!id2registration.ContainsKey(hashedId))
            {
                return null;
            }

            return id2registration[hashedId];
        }
        public async override Task<Result> GetResultObjectByTestId(string testId)
        {
            var id = testing2lastresult[testId];
            return await GetResultObject(id);
        }
        public async override Task<IEnumerable<string>> ListAllTestsKeys()
        {
            return testing2lastresult.Keys;
        }

    }
}
