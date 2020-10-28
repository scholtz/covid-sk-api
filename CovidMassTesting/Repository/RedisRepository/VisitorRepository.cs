using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
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
        private readonly string REDIS_KEY_VISITORS_OBJECTS = "VISITOR";
        private readonly string REDIS_KEY_TEST2VISITOR = "TEST2VISITOR";
        private readonly string REDIS_KEY_PERSONAL_NUMBER2VISITOR = "PNUM2VISITOR";
        private readonly IEmailSender emailSender;
        public VisitorRepository(
            IConfiguration configuration,
            ILogger<VisitorRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
            this.emailSender = emailSender;
        }
        public async Task<Visitor> Add(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor.Id = await CreateNewVisitorId();
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
            return await Set(visitor);
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
        public virtual async Task<Visitor> Set(Visitor visitor)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(visitor);
            logger.LogInformation($"Setting object {visitor.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            if (!await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", visitor.Id.ToString(CultureInfo.InvariantCulture), encoded, true))
            {
                throw new Exception("Error creating record in the database");
            }
            return visitor;
        }
        public virtual async Task MapTestingSetToVisitorCode(int codeInt, string testCodeClear)
        {
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", testCodeClear, codeInt);
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
            visitor.Result = state;
            if (state == "test-not-processed")
            {
                visitor.TestingSet = testingSet;
            }
            await Set(visitor);

            switch (state)
            {
                case "test-not-processed":
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingInProcessEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",

                    });
                    break;
                case "positive":
                case "negative":
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingResultEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",

                    });
                    break;
                case "not-submitted":
                case "submitting":
                case "error":
                case "test-not-taken":
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

    }
}
