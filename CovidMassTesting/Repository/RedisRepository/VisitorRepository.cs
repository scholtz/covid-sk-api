using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    public class VisitorRepository : IVisitorRepository
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<VisitorRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IPlaceRepository placeRepository;
        private readonly string REDIS_KEY_VISITORS_OBJECTS = "VISITOR";
        private readonly string REDIS_KEY_TEST2VISITOR = "TEST2VISITOR";
        private readonly string REDIS_KEY_PERSONAL_NUMBER2VISITOR = "PNUM2VISITOR";
        private readonly string REDIS_KEY_DOCUMENT_QUEUE = "DOCUMENT_QUEUE";
        private readonly IEmailSender emailSender;
        public VisitorRepository(
            IConfiguration configuration,
            ILogger<VisitorRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            IPlaceRepository placeRepository
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
            this.emailSender = emailSender;
            this.placeRepository = placeRepository;
        }
        public async Task<Visitor> Add(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor.Id = await CreateNewVisitorId();
            visitor.LastUpdate = DateTimeOffset.Now;
            visitor.Result = TestResult.NotTaken;
            visitor.TestingSet = "";

            var code = visitor.Id.ToString();
            switch (visitor.PersonType)
            {
                case "idcard":
                case "child":
                    if (!string.IsNullOrEmpty(visitor.RC))
                    {
                        await MapPersonalNumberToVisitorCode(visitor.RC, visitor.Id);
                    }
                    break;
                case "foreign":
                    if (!string.IsNullOrEmpty(visitor.Passport))
                    {
                        await MapPersonalNumberToVisitorCode(visitor.Passport, visitor.Id);
                    }
                    break;
            }
            await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorRegistrationEmail()
            {
                Code = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                Name = $"{visitor.FirstName} {visitor.LastName}",
                ///@TODO BAR CODE
            });
            return await Set(visitor, true);
        }
        public virtual async Task<bool> Remove(int id)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", id.ToString(CultureInfo.InvariantCulture));
            return true;
        }


        public virtual async Task<Visitor> Get(int code)
        {
            logger.LogInformation($"Visitor loaded from database: {code.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", code.ToString());
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(decoded);
        }

        public virtual async Task<Visitor> Set(Visitor visitor, bool mustBeNew)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.LastUpdate = DateTimeOffset.Now;

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            logger.LogInformation($"Setting object {visitor.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", visitor.Id.ToString(CultureInfo.InvariantCulture), encoded, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception("Error creating record in the database");
            }
            if (!string.IsNullOrEmpty(visitor.TestingSet))
            {
                await MapTestingSetToVisitorCode(visitor.Id, visitor.TestingSet);
            }
            return visitor;
        }
        public virtual async Task MapTestingSetToVisitorCode(int codeInt, string testCodeClear)
        {
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", testCodeClear, codeInt);
        }
        public virtual async Task UnMapTestingSet(string testCodeClear)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", testCodeClear);
        }
        public virtual Task<int?> GETVisitorCodeFromTesting(string testCodeClear)
        {
            return redisCacheClient.Db0.HashGetAsync<int?>(
                $"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}",
                testCodeClear
            );
        }
        public virtual async Task MapPersonalNumberToVisitorCode(string personalNumber, int visitorCode)
        {
            await redisCacheClient.Db0.HashSetAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash(),
                visitorCode
            );
        }
        public virtual async Task UnMapPersonalNumber(string personalNumber)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}", Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash());
        }
        public virtual Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {
            return redisCacheClient.Db0.HashGetAsync<int?>(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash()
            );
        }
        public async Task<string> ConnectVisitorToTest(int codeInt, string testCodeClear)
        {
            await MapTestingSetToVisitorCode(codeInt, testCodeClear);
            await UpdateTestingState(codeInt, "test-not-processed", testCodeClear);
            return testCodeClear;
        }

        public Task<bool> UpdateTestingState(int code, string state)
        {
            return UpdateTestingState(code, state, "");
        }
        public async Task<bool> UpdateTestingState(int code, string state, string testingSet = "")
        {
            logger.LogInformation($"Updating state for {code.GetHashCode()}");
            var visitor = await Get(code);
            if (visitor == null) throw new Exception($"Visitor with code {code} not found");
            if (visitor.Result == state)
            {
                // repeated requests should not send emails
                return true;
            }

            visitor.Result = state;
            visitor.LastUpdate = DateTimeOffset.Now;
            if (state == "test-not-processed")
            {
                visitor.TestingSet = testingSet;
            }
            await Set(visitor, false);

            try
            {
                // update slots stats
                switch (state)
                {
                    case TestResult.PositiveWaitingForCertificate:
                        await placeRepository.IncrementPlaceSick(visitor.ChosenPlaceId);
                        break;
                    case TestResult.NegativeWaitingForCertificate:
                        await placeRepository.IncrementPlaceHealthy(visitor.ChosenPlaceId);
                        break;
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
            }
            try
            {
                // update slots stats
                switch (state)
                {
                    case TestResult.PositiveWaitingForCertificate:
                        await AddToDocQueue(visitor.TestingSet);
                        break;
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
            }
            // send email
            switch (state)
            {
                case TestResult.TestMustBeRepeated:
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingToBeRepeatedEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                    });
                    break;
                case TestResult.TestIsBeingProcessing:
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingInProcessEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",

                    });
                    break;
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.NegativeWaitingForCertificate:
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingResultEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",

                    });
                    break;
                default:
                    break;
            }
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
        public async Task<bool> RemoveTest(int code, string pass)
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
            if (visitor.Result != "negative") throw new Exception("Osobné údaje môžu byť vymazané iba pre testy s negatívnym výsledkom");

            await Remove(visitor.Id);
            if (!string.IsNullOrEmpty(visitor.TestingSet))
            {
                await UnMapTestingSet(visitor.TestingSet);
            }

            switch (visitor.PersonType)
            {
                case "idcard":
                case "child":
                    if (!string.IsNullOrEmpty(visitor.RC))
                    {
                        await UnMapPersonalNumber(visitor.RC);
                    }
                    break;
                case "foreign":
                    if (!string.IsNullOrEmpty(visitor.Passport))
                    {
                        await UnMapPersonalNumber(visitor.Passport);
                    }
                    break;
            }

            await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}",
                new Model.Email.PersonalDataRemovedEmail()
                {
                    Name = $"{visitor.FirstName} {visitor.LastName}",
                });

            return true;
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
            return redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}");
        }

        public Task<Visitor> GetVisitor(int codeInt)
        {
            return Get(codeInt);
        }
        public async Task<Visitor> GetVisitorByPersonalNumber(string personalNumber)
        {
            var code = await GETVisitorCodeFromPersonalNumber(personalNumber);
            if (!code.HasValue) throw new Exception("Nezname rodne cislo");
            return await Get(code.Value);
        }

        public async Task<Result> SetTestResult(string testCode, string result)
        {
            var visitorCode = await GETVisitorCodeFromTesting(testCode);
            if (!visitorCode.HasValue) throw new Exception("Unable to find visitor code from test code. Are you sure test code is correct?");

            await UpdateTestingState(visitorCode.Value, result);
            return new Result()
            {
                State = result
            };
        }

        public virtual Task<bool> AddToDocQueue(string testId)
        {
            return redisCacheClient.Db0.SortedSetAddAsync($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", testId, DateTimeOffset.UtcNow.Ticks);
        }
        public virtual Task<bool> RemoveFromDocQueue(string testId)
        {
            return redisCacheClient.Db0.SortedSetRemoveAsync($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", testId);
        }
        public virtual async Task<string> GetFirstItemFromQueue()
        {
            return (await redisCacheClient.Db0.SortedSetRangeByScoreAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", take: 1, skip: 0)).FirstOrDefault();
        }
        public async Task<Visitor> GetNextTest()
        {
            var firstTest = await GetFirstItemFromQueue();
            if (firstTest == null) throw new Exception("No test in queue");
            var visitor = await GETVisitorCodeFromTesting(firstTest);
            if (!visitor.HasValue)
            {
                logger.LogInformation($"Unable to match test {firstTest} to visitor");
                await RemoveFromDocQueue(firstTest);
                return await GetNextTest();
            }
            return await Get(visitor.Value);
        }
    }
}
