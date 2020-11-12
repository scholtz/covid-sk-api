using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.RedisRepository
{
    /// <summary>
    /// Visitor repository holds data about visitors
    /// </summary>
    public class VisitorRepository : IVisitorRepository
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<VisitorRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IPlaceRepository placeRepository;
        private readonly ISlotRepository slotRepository;
        private readonly IUserRepository userRepository;
        private readonly string REDIS_KEY_VISITORS_OBJECTS = "VISITOR";
        private readonly string REDIS_KEY_TEST2VISITOR = "TEST2VISITOR";
        private readonly string REDIS_KEY_PERSONAL_NUMBER2VISITOR = "PNUM2VISITOR";
        private readonly string REDIS_KEY_DOCUMENT_QUEUE = "DOCUMENT_QUEUE";
        private readonly IEmailSender emailSender;
        private readonly ISMSSender smsSender;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        /// <param name="placeRepository"></param>
        /// <param name="slotRepository"></param>
        /// <param name="userRepository"></param>
        public VisitorRepository(
            IConfiguration configuration,
            ILogger<VisitorRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            ISlotRepository slotRepository,
            IUserRepository userRepository
            )
        {
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
            this.emailSender = emailSender;
            this.smsSender = smsSender;
            this.placeRepository = placeRepository;
            this.slotRepository = slotRepository;
            this.userRepository = userRepository;
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

            var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
            var slot = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);

            await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorRegistrationEmail()
            {
                Code = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                Name = $"{visitor.FirstName} {visitor.LastName}",
                Date = slot.Time.ToString("dd.MM.yyyy H:mm"),
                Place = place.Name,
                PlaceDescription = place.Description
                ///@TODO BAR CODE
            });

            if (!string.IsNullOrEmpty(visitor.Phone))
            {
                await smsSender.SendSMS(visitor.Phone, new Model.SMS.RegistrationSMS()
                {
                    Code = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                    Name = $"{visitor.FirstName} {visitor.LastName}",
                    Date = slot.Time.ToString("dd.MM.yyyy H:mm"),
                    Place = place.Name
                });
            }
            return await Set(visitor, true);
        }
        /// <summary>
        /// Remove visitor from redis
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<bool> Remove(int id)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", id.ToString(CultureInfo.InvariantCulture));
            return true;
        }

        /// <summary>
        /// Decode visitor data from database
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public virtual async Task<Visitor> Get(int code)
        {
            logger.LogInformation($"Visitor loaded from database: {code.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", code.ToString());
            if (string.IsNullOrEmpty(encoded)) return null;
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(decoded);
        }
        /// <summary>
        /// Encode visitor data and store to database
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
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
                    case TestResult.NegativeWaitingForCertificate:
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

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.TestToRepeatSMS()
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });
                    }

                    break;
                case TestResult.TestIsBeingProcessing:
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingInProcessEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                    });

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.TestIsInProcessingSMS()
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });
                    }
                    break;
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.NegativeWaitingForCertificate:
                    await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}", new Model.Email.VisitorTestingResultEmail()
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",

                    });
                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.TestIsFinishedSMS()
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });
                    }
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
            if (visitor.Result != TestResult.NegativeCertificateTaken) throw new Exception("Osobné údaje môžu byť vymazané iba pre testy s negatívnym výsledkom po prevzatí certifikátu");

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

            if (!string.IsNullOrEmpty(visitor.Phone))
            {
                await smsSender.SendSMS(visitor.Phone, new Model.SMS.PersonalDataRemovedSMS()
                {
                    Name = $"{visitor.FirstName} {visitor.LastName}",
                });
            }
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
        public async Task<bool> RemoveFromDocQueueAndSetTestStateAsTaken(string testId)
        {
            var first = await GetFirstItemFromQueue();
            if (first != testId) throw new Exception("You can remove only first item from the queue");
            var visitorCode = await GETVisitorCodeFromTesting(testId);
            if (!visitorCode.HasValue) throw new Exception($"Visitor with code {visitorCode} not found");
            var visitor = await Get(visitorCode.Value);
            switch (visitor.Result)
            {
                case TestResult.NegativeWaitingForCertificate:
                    await SetTestResult(testId, TestResult.NegativeCertificateTaken);
                    break;
                case TestResult.PositiveWaitingForCertificate:
                    await SetTestResult(testId, TestResult.PositiveCertificateTaken);
                    break;
            }
            return await RemoveFromDocQueue(testId);
        }
        public virtual async Task<string> GetFirstItemFromQueue()
        {
            return (await redisCacheClient.Db0.SortedSetRangeByScoreAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", take: 1, skip: 0)).FirstOrDefault();
        }
        public async Task<Visitor> GetNextTest()
        {
            var firstTest = await GetFirstItemFromQueue();
            if (firstTest == null) return null;
            var visitor = await GETVisitorCodeFromTesting(firstTest);
            if (!visitor.HasValue)
            {
                logger.LogInformation($"Unable to match test {firstTest} to visitor");
                await RemoveFromDocQueue(firstTest);
                return await GetNextTest();
            }
            return await Get(visitor.Value);
        }

        /// <summary>
        /// Public registration
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="managerEmail"></param>
        /// <returns></returns>
        public async Task<Visitor> Register(Visitor visitor, string managerEmail)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            if (!string.IsNullOrEmpty(managerEmail))
            {
                // register to manager place and nearest slot

                UserPublic manager = await userRepository.GetPublicUser(managerEmail);

                visitor.ChosenPlaceId = manager.Place;
                if (string.IsNullOrEmpty(visitor.ChosenPlaceId)) throw new Exception("Vyberte si najskôr miesto kde sa nachádzate.");
                var currentSlot = await slotRepository.GetCurrentSlot(manager.Place);
                if (currentSlot == null) throw new Exception("Unable to select testing slot.");
                visitor.ChosenSlot = currentSlot.SlotId;
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(visitor.Email);
                visitor.Email = addr.Address;
            }
            catch
            {
                visitor.Email = "";
            }

            visitor.Phone = FormatPhone(visitor.Phone);
            if (!IsPhoneNumber(visitor.Phone))
            {
                visitor.Phone = "";
            }

            var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
            if (place == null) { throw new Exception("We are not able to find chosen testing place"); }
            var slotM = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
            if (slotM == null) { throw new Exception("We are not able to find chosen slot"); }
            var slotH = await slotRepository.GetHourSlot(visitor.ChosenPlaceId, slotM.HourSlotId);
            if (slotH == null) { throw new Exception("We are not able to find chosen hour slot"); }
            var slotD = await slotRepository.GetDaySlot(visitor.ChosenPlaceId, slotH.DaySlotId);

            if (slotM.Registrations >= place.LimitPer5MinSlot)
            {
                throw new Exception("Tento 5-minútový časový slot má kapacitu zaplnenú.");
            }
            if (slotH.Registrations >= place.LimitPer1HourSlot)
            {
                throw new Exception("Tento hodinový časový slot má kapacitu zaplnenú.");
            }
            Visitor previous = null;
            try
            {
                switch (visitor.PersonType)
                {
                    case "idcard":
                    case "child":
                        if (!string.IsNullOrEmpty(visitor.RC))
                        {
                            previous = await GetVisitorByPersonalNumber(visitor.RC);
                        }
                        break;
                    case "foreign":
                        if (!string.IsNullOrEmpty(visitor.Passport))
                        {
                            previous = await GetVisitorByPersonalNumber(visitor.Passport);
                        }
                        break;
                }
            }
            catch
            {

            }
            if (previous == null)
            {
                // new registration

                var ret = await Add(visitor);

                await slotRepository.IncrementRegistration5MinSlot(slotM);
                await slotRepository.IncrementRegistrationHourSlot(slotH);
                await slotRepository.IncrementRegistrationDaySlot(slotD);
                await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);

                return ret;
            }
            else
            {
                // update registration
                visitor.Id = previous.Id; // bar code does not change on new registration with the same personal number
                var slot = slotM;

                var ret = await Set(visitor, false);
                if (previous.ChosenPlaceId != visitor.ChosenPlaceId)
                {
                    await placeRepository.DecrementPlaceRegistrations(previous.ChosenPlaceId);
                    await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);
                }
                var code = visitor.Id.ToString();
                var codeFormatted = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}";

                await emailSender.SendEmail(visitor.Email, $"{visitor.FirstName} {visitor.LastName}",
                    new Model.Email.VisitorChangeRegistrationEmail()
                    {
                        Code = codeFormatted,
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                        Date = slot.Time.ToString("dd.MM.yyyy H:mm"),
                        Place = place.Name,
                        PlaceDescription = place.Description
                        ///@TODO BAR CODE
                    });

                if (!string.IsNullOrEmpty(visitor.Phone))
                {
                    await smsSender.SendSMS(visitor.Phone, new Model.SMS.RegistrationChangedSMS()
                    {
                        Code = codeFormatted,
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                        Date = slot.Time.ToString("dd.MM.yyyy H:mm"),
                        Place = place.Name
                    });
                }

                if (previous.ChosenSlot != visitor.ChosenSlot)
                {
                    try
                    {

                        var slotMPrev = await slotRepository.Get5MinSlot(previous.ChosenPlaceId, previous.ChosenSlot);
                        var slotHPrev = await slotRepository.GetHourSlot(previous.ChosenPlaceId, slotMPrev.HourSlotId);
                        var slotDPrev = await slotRepository.GetDaySlot(previous.ChosenPlaceId, slotHPrev.DaySlotId);

                        await slotRepository.DecrementRegistration5MinSlot(slotM);
                        await slotRepository.DecrementRegistrationHourSlot(slotH);
                        await slotRepository.DecrementRegistrationDaySlot(slotD);
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, exc.Message);
                    }
                    await slotRepository.IncrementRegistration5MinSlot(slotM);
                    await slotRepository.IncrementRegistrationHourSlot(slotH);
                    await slotRepository.IncrementRegistrationDaySlot(slotD);
                }

                return ret;
            }
        }
        /// <summary>
        /// Format phone to slovak standard
        /// 
        /// 0800 123 456 convers to +421800123456
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string FormatPhone(string number)
        {
            if (number == null) number = "";
            number = number.Replace(" ", "");
            number = number.Replace("\t", "");
            if (number.StartsWith("00")) number = "+" + number.Substring(2);
            if (number.StartsWith("0")) number = "+421" + number.Substring(1);
            return number;
        }
        /// <summary>
        /// Validates the phone number +421800123456
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{12})$").Success;
        }
    }
}
