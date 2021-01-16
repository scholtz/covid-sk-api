using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
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
        private readonly IStringLocalizer<VisitorRepository> localizer;
        private readonly IConfiguration configuration;
        private readonly ILogger<VisitorRepository> logger;
        private readonly IRedisCacheClient redisCacheClient;
        private readonly IPlaceRepository placeRepository;
        private readonly ISlotRepository slotRepository;
        private readonly IUserRepository userRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly string REDIS_KEY_VISITORS_OBJECTS = "VISITOR";
        private readonly string REDIS_KEY_RESULTS_OBJECTS = "RESULTS";
        private readonly string REDIS_KEY_TEST2VISITOR = "TEST2VISITOR";
        private readonly string REDIS_KEY_PERSONAL_NUMBER2VISITOR = "PNUM2VISITOR";
        private readonly string REDIS_KEY_DOCUMENT_QUEUE = "DOCUMENT_QUEUE";
        private readonly IEmailSender emailSender;
        private readonly ISMSSender smsSender;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="redisCacheClient"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        /// <param name="placeRepository"></param>
        /// <param name="slotRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="userRepository"></param>
        public VisitorRepository(
            IStringLocalizer<VisitorRepository> localizer,
            IConfiguration configuration,
            ILogger<VisitorRepository> logger,
            IRedisCacheClient redisCacheClient,
            IEmailSender emailSender,
            ISMSSender smsSender,
            IPlaceRepository placeRepository,
            ISlotRepository slotRepository,
            IPlaceProviderRepository placeProviderRepository,
            IUserRepository userRepository
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.redisCacheClient = redisCacheClient;
            this.configuration = configuration;
            this.emailSender = emailSender;
            this.smsSender = smsSender;
            this.placeRepository = placeRepository;
            this.slotRepository = slotRepository;
            this.userRepository = userRepository;
            this.placeProviderRepository = placeProviderRepository;
        }
        /// <summary>
        /// Creates new visitor registration
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
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
            visitor.Language = CultureInfo.CurrentCulture.Name;

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

            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;


            var attachments = new List<SendGrid.Helpers.Mail.Attachment>();

            try
            {
                //var product = await placeRepository.GetPlaceProduct();
                var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
                var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);

                var pdf = GenerateRegistrationPDF(visitor, pp?.CompanyName, place?.Address, product?.Name);
                attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                {
                    Content = Convert.ToBase64String(pdf),
                    Filename = $"reg-{visitor.LastName}{visitor.FirstName}-{slot.Time.ToString("MMdd")}.pdf",
                    Type = "application/pdf",
                    Disposition = "attachment"
                });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error generating file");
            }

            await emailSender.SendEmail(
                localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                visitor.Email,
                $"{visitor.FirstName} {visitor.LastName}",
                new Model.Email.VisitorRegistrationEmail(visitor.Language)
                {
                    Code = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                    Name = $"{visitor.FirstName} {visitor.LastName}",
                    Date = slot.Time.ToString("dd.MM.yyyy H:mm"),
                    Place = place.Name,
                    PlaceDescription = place.Description
                }, attachments);

            if (!string.IsNullOrEmpty(visitor.Phone))
            {
                await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(
                    string.Format(
                        Repository_RedisRepository_VisitorRepository.Dear__0____1__is_your_registration_code__Show_this_code_at_the_covid_sampling_place__3__on__2_,
                        $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                        $"{visitor.FirstName} {visitor.LastName}",
                        slot.Time.ToString("dd.MM.yyyy H:mm"),
                        place.Name
                )));
            }
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUICulture;
            return await SetVisitor(visitor, true);
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
        /// <param name="codeInt"></param>
        /// <returns></returns>
        public virtual async Task<Visitor> GetVisitor(int codeInt)
        {
            logger.LogInformation($"Visitor loaded from database: {codeInt.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", codeInt.ToString());
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
        public virtual async Task<Visitor> SetVisitor(Visitor visitor, bool mustBeNew)
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
        /// <summary>
        /// Maps testing code to visitor code
        /// </summary>
        /// <param name="codeInt"></param>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public virtual async Task MapTestingSetToVisitorCode(int codeInt, string testCodeClear)
        {
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", testCodeClear, codeInt);
        }
        /// <summary>
        /// Removes testing code
        /// </summary>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public virtual async Task UnMapTestingSet(string testCodeClear)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", testCodeClear);
        }
        /// <summary>
        /// Returns visitor code from testing code
        /// </summary>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public virtual Task<int?> GETVisitorCodeFromTesting(string testCodeClear)
        {
            return redisCacheClient.Db0.HashGetAsync<int?>(
                $"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}",
                testCodeClear
            );
        }
        /// <summary>
        /// Maps personal number to visitor code
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <param name="visitorCode"></param>
        /// <returns></returns>
        public virtual async Task MapPersonalNumberToVisitorCode(string personalNumber, int visitorCode)
        {
            await redisCacheClient.Db0.HashSetAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash(),
                visitorCode
            );
        }
        /// <summary>
        /// Removes personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public virtual async Task UnMapPersonalNumber(string personalNumber)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}", Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash());
        }
        /// <summary>
        /// Returns visitor code from personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public virtual Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {
            return redisCacheClient.Db0.HashGetAsync<int?>(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash()
            );
        }
        /// <summary>
        /// Map visitor code to testing code
        /// </summary>
        /// <param name="codeInt"></param>
        /// <param name="testCodeClear"></param>
        /// <returns></returns>
        public async Task<string> ConnectVisitorToTest(int codeInt, string testCodeClear)
        {
            await MapTestingSetToVisitorCode(codeInt, testCodeClear);
            await UpdateTestingState(codeInt, "test-not-processed", testCodeClear);
            return testCodeClear;
        }
        /// <summary>
        /// Updates testing state
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public Task<bool> UpdateTestingState(int code, string state)
        {
            return UpdateTestingState(code, state, "");
        }
        /// <summary>
        /// Updates testing state
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="testingSet"></param>
        /// <returns></returns>
        public async Task<bool> UpdateTestingState(int code, string state, string testingSet = "")
        {
            logger.LogInformation($"Updating state for {code.GetHashCode()}");
            var visitor = await GetVisitor(code);
            if (visitor == null) throw new Exception(string.Format(localizer[Repository_RedisRepository_VisitorRepository.Visitor_with_code__0__not_found].Value, code));
            if (visitor.Result == state)
            {
                // repeated requests should not send emails
                return true;
            }

            visitor.Result = state;
            visitor.LastUpdate = DateTimeOffset.Now;
            if (state == TestResult.TestIsBeingProcessing)
            {
                visitor.TestingTime = visitor.LastUpdate;
            }
            if (state == "test-not-processed")
            {
                visitor.TestingSet = testingSet;
            }
            await SetVisitor(visitor, false);

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
                    var oldCulture = CultureInfo.CurrentCulture;
                    var oldUICulture = CultureInfo.CurrentUICulture;
                    var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                    CultureInfo.CurrentCulture = specifiedCulture;
                    CultureInfo.CurrentUICulture = specifiedCulture;

                    await emailSender.SendEmail(
                        localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                        visitor.Email,
                        $"{visitor.FirstName} {visitor.LastName}",
                        new Model.Email.VisitorTestingToBeRepeatedEmail(visitor.Language)
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(
                            string.Format(Repository_RedisRepository_VisitorRepository.Dear__0___there_were_some_technical_issues_with_your_test__Please_visit_the_sampling_place_again_and_repeat_the_test_procedure__You_can_use_the_same_registration_as_before_,
                            $"{visitor.FirstName} {visitor.LastName}")));

                    }

                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;

                    break;
                case TestResult.TestIsBeingProcessing:
                    oldCulture = CultureInfo.CurrentCulture;
                    oldUICulture = CultureInfo.CurrentUICulture;
                    specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                    CultureInfo.CurrentCulture = specifiedCulture;
                    CultureInfo.CurrentUICulture = specifiedCulture;

                    await emailSender.SendEmail(
                        localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                        visitor.Email,
                        $"{visitor.FirstName} {visitor.LastName}",
                        new Model.Email.VisitorTestingInProcessEmail(visitor.Language)
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {

                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(string.Format(
                            Repository_RedisRepository_VisitorRepository.Dear__0___your_test_is_in_processing__Please_wait_for_further_instructions_in_next_sms_message_,
                            $"{visitor.FirstName} {visitor.LastName}"
                            )));

                    }
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;
                    break;
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.NegativeWaitingForCertificate:
                    oldCulture = CultureInfo.CurrentCulture;
                    oldUICulture = CultureInfo.CurrentUICulture;
                    specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                    CultureInfo.CurrentCulture = specifiedCulture;
                    CultureInfo.CurrentUICulture = specifiedCulture;
                    var attachments = new List<SendGrid.Helpers.Mail.Attachment>();
                    try
                    {
                        var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                        //var product = await placeRepository.GetPlaceProduct();
                        var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
                        var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);


                        var result = await SetResult(new VerificationData()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                            Product = product?.Name,
                            TestingAddress = place?.Address,
                            Result = visitor.Result,
                            TestingEntity = pp?.CompanyName
                        }, true);
                        visitor.VerificationId = result.Id;
                        var pdf = GenerateResultPDF(visitor, pp?.CompanyName, place?.Address, product?.Name, result.Id);
                        attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                        {
                            Content = Convert.ToBase64String(pdf),
                            Filename = $"{visitor.LastName}{visitor.FirstName}-{visitor.TestingTime.ToString("MMdd")}.pdf",
                            Type = "application/pdf",
                            Disposition = "attachment"
                        });
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "Error generating file");
                    }
                    await emailSender.SendEmail(
                        localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                        visitor.Email,
                        $"{visitor.FirstName} {visitor.LastName}",
                        new Model.Email.VisitorTestingResultEmail(visitor.Language)
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",

                        },
                        attachments
                        );
                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {

                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(string.Format(Repository_RedisRepository_VisitorRepository.Dear__0___your_test_result_has_been_processed__You_can_check_the_result_online__Please_come_to_take_the_certificate_, $"{visitor.FirstName} {visitor.LastName}")));

                    }
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;
                    break;
                default:
                    break;
            }
            return true;
        }
        /// <summary>
        /// Returns test by code and password
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public async Task<Result> GetTest(int code, string pass)
        {
            if (string.IsNullOrEmpty(pass))
            {
                throw new ArgumentException(localizer[Repository_RedisRepository_VisitorRepository.Last_4_digits_of_personal_number_or_declared_passport_for_foreigner_at_registration_must_not_be_empty].Value);
            }
            if (pass.Length < 4)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            var visitor = await GetVisitor(code);
            if (visitor == null)
            {
                throw new Exception("Skontrolujte prosím správne zadanie kódu registrácie.");
            }
            if (visitor.RC?.Length > 4 && !visitor.RC.Trim().EndsWith(pass.Trim(), true, CultureInfo.InvariantCulture))
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            if (visitor.Passport?.Length > 4 && !visitor.Passport.Trim().EndsWith(pass.Trim(), true, CultureInfo.InvariantCulture))
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            return new Result { State = visitor.Result, VerificationId = visitor.VerificationId };
        }
        /// <summary>
        /// Deletes the test
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public async Task<bool> RemoveTest(int code, string pass)
        {
            if (string.IsNullOrEmpty(pass))
            {
                throw new ArgumentException(localizer[Repository_RedisRepository_VisitorRepository.Last_4_digits_of_personal_number_or_declared_passport_for_foreigner_at_registration_must_not_be_empty].Value);
            }
            if (pass.Length < 4)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            var visitor = await GetVisitor(code);
            if (visitor == null)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Test_does_not_exists].Value);
            }
            if (visitor.RC?.Length > 4 && !visitor.RC.Trim().EndsWith(pass.Trim(), true, CultureInfo.InvariantCulture))
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            if (visitor.Passport?.Length > 4 && !visitor.Passport.Trim().EndsWith(pass.Trim(), true, CultureInfo.InvariantCulture))
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            if (visitor.Result != TestResult.NegativeCertificateTaken) throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Personal_data_may_be_deleted_only_after_the_test_has_proven_negative_result_and_person_receives_the_certificate_].Value);

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

            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;

            await emailSender.SendEmail(
                localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                visitor.Email,
                $"{visitor.FirstName} {visitor.LastName}",
                new Model.Email.PersonalDataRemovedEmail(visitor.Language)
                {
                    Name = $"{visitor.FirstName} {visitor.LastName}",
                });

            if (!string.IsNullOrEmpty(visitor.Phone))
            {

                await smsSender.SendSMS(visitor.Phone,
                    new Model.SMS.Message(string.Format(localizer[Repository_RedisRepository_VisitorRepository.Dear__0__We_have_removed_your_personal_data_from_the_database__Thank_you_for_taking_the_covid_test].Value, $"{visitor.FirstName} {visitor.LastName}"))
                );

            }
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUICulture;
            return true;
        }
        /// <summary>
        /// Creates new unique visitor id
        /// </summary>
        /// <returns></returns>
        protected async Task<int> CreateNewVisitorId()
        {
            var existingIds = await ListAllKeys();
            using var rand = new RandomGenerator();
            var next = rand.Next(100000000, 999999999);
            while (existingIds.Contains(next.ToString(CultureInfo.InvariantCulture)))
            {
                next = rand.Next(100000000, 999999999);
            }
            return next;
        }
        /// <summary>
        /// Lists all keys
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<string>> ListAllKeys()
        {
            return redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}");
        }
        /// <summary>
        /// Returns visitor by personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public async Task<Visitor> GetVisitorByPersonalNumber(string personalNumber)
        {
            var code = await GETVisitorCodeFromPersonalNumber(personalNumber);
            if (!code.HasValue) throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Unknown_personal_number].Value);
            return await GetVisitor(code.Value);
        }
        /// <summary>
        /// Set test result
        /// </summary>
        /// <param name="testCode"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public async Task<Result> SetTestResult(string testCode, string result)
        {
            var visitorCode = await GETVisitorCodeFromTesting(testCode);
            if (!visitorCode.HasValue) throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Unable_to_find_visitor_code_from_test_code__Are_you_sure_test_code_is_correct_].Value);

            await UpdateTestingState(visitorCode.Value, result);
            return new Result()
            {
                State = result
            };
        }
        /// <summary>
        /// Add test to document queue
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        public virtual Task<bool> AddToDocQueue(string testId)
        {
            return redisCacheClient.Db0.SortedSetAddAsync($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", testId, DateTimeOffset.UtcNow.Ticks);
        }
        /// <summary>
        /// Removes test from document queue
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        public virtual Task<bool> RemoveFromDocQueue(string testId)
        {
            return redisCacheClient.Db0.SortedSetRemoveAsync($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", testId);
        }
        /// <summary>
        /// Removes document from queue and sets the test as taken
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        public async Task<bool> RemoveFromDocQueueAndSetTestStateAsTaken(string testId)
        {
            var first = await GetFirstItemFromQueue();
            if (first != testId) throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.You_can_remove_only_first_item_from_the_queue].Value);
            var visitorCode = await GETVisitorCodeFromTesting(testId);
            if (!visitorCode.HasValue) throw new Exception(string.Format(localizer[Repository_RedisRepository_VisitorRepository.Visitor_with_code__0__not_found].Value, visitorCode));
            var visitor = await GetVisitor(visitorCode.Value);
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
        /// <summary>
        /// Seek first item from queue
        /// </summary>
        /// <returns></returns>
        public virtual async Task<string> GetFirstItemFromQueue()
        {
            return (await redisCacheClient.Db0.SortedSetRangeByScoreAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}", take: 1, skip: 0)).FirstOrDefault();
        }
        /// <summary>
        /// Fetch next test
        /// </summary>
        /// <returns></returns>
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
            return await GetVisitor(visitor.Value);
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
            if (place == null) { throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.We_are_not_able_to_find_chosen_testing_place].Value); }
            var slotM = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
            if (slotM == null) { throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.We_are_not_able_to_find_chosen_5_min__slot].Value); }
            var slotH = await slotRepository.GetHourSlot(visitor.ChosenPlaceId, slotM.HourSlotId);
            if (slotH == null) { throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.We_are_not_able_to_find_chosen_hour_slot].Value); }
            var slotD = await slotRepository.GetDaySlot(visitor.ChosenPlaceId, slotH.DaySlotId);

            if (slotM.Registrations >= place.LimitPer5MinSlot)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.This_5_minute_time_slot_has_reached_the_capacity_].Value);
            }
            if (slotH.Registrations >= place.LimitPer1HourSlot)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.This_1_hour_time_slot_has_reached_the_capacity_].Value);
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
                logger.LogInformation($"New registration");

                var ret = await Add(visitor);

                await slotRepository.IncrementRegistration5MinSlot(slotM);
                await slotRepository.IncrementRegistrationHourSlot(slotH);
                await slotRepository.IncrementRegistrationDaySlot(slotD);
                await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);

                logger.LogInformation($"Incremented: M-{slotM.SlotId}, {slotH.SlotId}, {slotD.SlotId}");

                return ret;
            }
            else
            {
                logger.LogInformation($"Update registration");
                // update registration
                visitor.Id = previous.Id; // bar code does not change on new registration with the same personal number
                var slot = slotM;
                visitor.Language = CultureInfo.CurrentCulture.Name;
                var ret = await SetVisitor(visitor, false);
                if (previous.ChosenPlaceId != visitor.ChosenPlaceId)
                {
                    await placeRepository.DecrementPlaceRegistrations(previous.ChosenPlaceId);
                    await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);
                }
                var code = visitor.Id.ToString();
                var codeFormatted = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}";

                var oldCulture = CultureInfo.CurrentCulture;
                var oldUICulture = CultureInfo.CurrentUICulture;
                var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                CultureInfo.CurrentCulture = specifiedCulture;
                CultureInfo.CurrentUICulture = specifiedCulture;

                var attachments = new List<SendGrid.Helpers.Mail.Attachment>();

                try
                {
                    //var product = await placeRepository.GetPlaceProduct();
                    var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
                    var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);

                    var pdf = GenerateRegistrationPDF(visitor, pp?.CompanyName, place?.Address, product?.Name);
                    attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                    {
                        Content = Convert.ToBase64String(pdf),
                        Filename = $"reg-{visitor.LastName}{visitor.FirstName}-{slotD.Time.ToString("MMdd")}.pdf",
                        Type = "application/pdf",
                        Disposition = "attachment"
                    });
                }
                catch (Exception exc)
                {
                    logger.LogError(exc, "Error generating file");
                }


                await emailSender.SendEmail(
                    localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                    visitor.Email,
                    $"{visitor.FirstName} {visitor.LastName}",
                    new Model.Email.VisitorChangeRegistrationEmail(visitor.Language)
                    {
                        Code = codeFormatted,
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                        Date = slot.Time.ToString("dd.MM.yyyy H:mm"),
                        Place = place.Name,
                        PlaceDescription = place.Description

                    }, attachments);

                if (!string.IsNullOrEmpty(visitor.Phone))
                {

                    await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(
                        string.Format(localizer[Repository_RedisRepository_VisitorRepository.Dear__0___we_have_updated_your_registration__1___Time___2___Place___3_].Value,
                        $"{visitor.FirstName} {visitor.LastName}",
                        codeFormatted,
                        slot.Time.ToString("dd.MM.yyyy H:mm"),
                        place.Name
                    )));

                }
                CultureInfo.CurrentCulture = oldCulture;
                CultureInfo.CurrentUICulture = oldUICulture;

                if (previous.ChosenSlot != visitor.ChosenSlot || previous.ChosenPlaceId != visitor.ChosenPlaceId)
                {
                    try
                    {

                        var slotMPrev = await slotRepository.Get5MinSlot(previous.ChosenPlaceId, previous.ChosenSlot);
                        var slotHPrev = await slotRepository.GetHourSlot(previous.ChosenPlaceId, slotMPrev.HourSlotId);
                        var slotDPrev = await slotRepository.GetDaySlot(previous.ChosenPlaceId, slotHPrev.DaySlotId);

                        await slotRepository.DecrementRegistration5MinSlot(slotMPrev);
                        await slotRepository.DecrementRegistrationHourSlot(slotHPrev);
                        await slotRepository.DecrementRegistrationDaySlot(slotDPrev);

                        logger.LogInformation($"Decremented: M-{slotMPrev.SlotId}, {slotHPrev.SlotId}, {slotDPrev.SlotId}");
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, exc.Message);
                    }
                    await slotRepository.IncrementRegistration5MinSlot(slotM);
                    await slotRepository.IncrementRegistrationHourSlot(slotH);
                    await slotRepository.IncrementRegistrationDaySlot(slotD);

                    logger.LogInformation($"Incremented: M-{slotM.SlotId}, {slotH.SlotId}, {slotD.SlotId}");
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
            if (number.StartsWith("00", true, CultureInfo.InvariantCulture)) number = "+" + number[2..];
            if (number.StartsWith("0", true, CultureInfo.InvariantCulture)) number = "+421" + number[1..];
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
        /// <summary>
        /// Deletes all data
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> DropAllData()
        {
            var ret = 0;
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}", item);
            }
            ret += (int)await redisCacheClient.Db0.SetRemoveAllAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}");
            return ret;
        }
        /// <summary>
        /// List Sick Visitors. Data Exporter person at the end of testing can fetch all info and deliver them to medical office
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListSickVisitors(int from = 0, int count = 9999999)
        {
            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys()).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null) continue;
                    if (visitor.Result == TestResult.PositiveCertificateTaken ||
                        visitor.Result == TestResult.PositiveWaitingForCertificate)
                    {
                        ret.Add(visitor);
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// Tests the storage
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> TestStorage()
        {
            using var rand = new RandomGenerator();
            var toSave = rand.Next(100000, 900000);
            await redisCacheClient.Db0.AddAsync("TEST", toSave);
            var ret = await redisCacheClient.Db0.GetAsync<int>("TEST");
            if (toSave != ret) throw new Exception("Storage does not work");
            return ret;
        }
        /// <summary>
        /// Creates html source code for pdf generation
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="testingEntity"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <param name="resultguid"></param>
        /// <returns></returns>
        public string GenerateResultHTML(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid)
        {
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;

            var data = new Model.Mustache.TestResult();
            switch (visitor.Result)
            {
                case TestResult.PositiveWaitingForCertificate:
                    data.Text = "Pozitívny";
                    data.Description = "Zostaňte prosím v karanténe minimálne 14 dní. Potom si vykonajte ďalší antigénový alebo PCR test aby ste mali istotu že vírus nebudete šíriť medzi ľudí.";
                    break;
                case TestResult.NegativeWaitingForCertificate:
                    data.Text = "Negatívny";
                    data.Description = "Aj keď test u Vás nepreukázal COVID, prosím zostaňte ostražitý. V prípade príznakov ako kašeľ, zvýšená teplota, alebo bolesť hlavy choďte prosím na ďalší test.";
                    break;
                default:
                    throw new Exception("Invalid state for PDF generation");
            }

            data.Name = $"{visitor.FirstName} {visitor.LastName}";
            data.Date = visitor.TestingTime.ToString("f");

            switch (visitor.PersonType)
            {
                case "idcard":
                case "child":
                    data.PersonalNumber = visitor.RC;
                    break;
                case "foreign":
                    data.PassportNumber = visitor.Passport;
                    break;
            }

            data.TestingAddress = placeAddress;
            data.TestingEntity = testingEntity;
            data.FrontedURL = configuration["FrontedURL"];
            data.ResultGUID = resultguid;
            data.VerifyURL = $"{configuration["FrontedURL"]}#/check/{data.ResultGUID}";
            data.Product = product;

            QRCoder.QRCodeGenerator qrGenerator = new QRCoder.QRCodeGenerator();
            QRCoder.QRCodeData qrCodeData = qrGenerator.CreateQrCode(data.VerifyURL, QRCoder.QRCodeGenerator.ECCLevel.Q);
            QRCoder.QRCode qrCode = new QRCoder.QRCode(qrCodeData);
            using var outData = new MemoryStream();
            qrCode.GetGraphic(20).Save(outData, System.Drawing.Imaging.ImageFormat.Png);
            var pngBytes = outData.ToArray();
            data.QRVerificationURL = Convert.ToBase64String(pngBytes).Replace("\n", "");

            var stubble = new Stubble.Core.Builders.StubbleBuilder().Build();
            var ret = stubble.Render(Resources.Repository_RedisRepository_VisitorRepository.TestResult, data);

            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUICulture;
            return ret;
        }

        /// <summary>
        /// Creates html source code for pdf generation
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="testingEntity"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public string GenerateRegistrationHTML(Visitor visitor, string testingEntity, string placeAddress, string product)
        {
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;

            var data = new Model.Mustache.TestRegistration();

            data.Name = $"{visitor.FirstName} {visitor.LastName}";

            data.Date = new DateTimeOffset(visitor.ChosenSlot, TimeSpan.Zero).ToString("f");

            switch (visitor.PersonType)
            {
                case "idcard":
                case "child":
                    data.PersonalNumber = visitor.RC;
                    break;
                case "foreign":
                    data.PassportNumber = visitor.Passport;
                    break;
            }

            data.TestingAddress = placeAddress;
            data.TestingEntity = testingEntity;
            data.FrontedURL = configuration["FrontedURL"];
            data.Product = product;

            BarcodeLib.Barcode b = new BarcodeLib.Barcode();
            var formatted = visitor.Id.ToString();
            if (formatted.Length == 9)
            {
                formatted = formatted.Substring(0, 3) + "-" + formatted.Substring(3, 3) + "-" + formatted.Substring(6, 3);
            }
            data.RegistrationCode = formatted;

            Image img = b.Encode(BarcodeLib.TYPE.CODE39, formatted, Color.Black, Color.White, 300, 120);
            using var outDataBar = new MemoryStream();
            img.Save(outDataBar, System.Drawing.Imaging.ImageFormat.Png);
            var barBytes = outDataBar.ToArray();
            data.BarCode = Convert.ToBase64String(barBytes).Replace("\n", "");

            QRCoder.QRCodeGenerator qrGenerator = new QRCoder.QRCodeGenerator();
            QRCoder.QRCodeData qrCodeData = qrGenerator.CreateQrCode(visitor.Id.ToString(), QRCoder.QRCodeGenerator.ECCLevel.Q);
            QRCoder.QRCode qrCode = new QRCoder.QRCode(qrCodeData);
            using var outData = new MemoryStream();
            qrCode.GetGraphic(20).Save(outData, System.Drawing.Imaging.ImageFormat.Png);
            var qrBytes = outData.ToArray();
            data.QRCode = Convert.ToBase64String(qrBytes).Replace("\n", "");

            var stubble = new Stubble.Core.Builders.StubbleBuilder().Build();
            var ret = stubble.Render(Resources.Repository_RedisRepository_VisitorRepository.TestRegistration, data);

            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUICulture;
            return ret;
        }

        /// <summary>
        /// Creates pdf from test result
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="testingEntity"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <param name="resultguid"></param>
        /// <returns></returns>
        public byte[] GenerateResultPDF(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid)
        {
            var html = GenerateResultHTML(visitor, testingEntity, placeAddress, product, resultguid);
            using var pdfStreamEncrypted = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(pdfStreamEncrypted,
                            new iText.Kernel.Pdf.WriterProperties()
                                    .SetStandardEncryption(
                                        Encoding.ASCII.GetBytes(visitor.RC),
                                        Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                                        iText.Kernel.Pdf.EncryptionConstants.ALLOW_PRINTING,
                                        iText.Kernel.Pdf.EncryptionConstants.ENCRYPTION_AES_256
                                    )

                         //                                    .SetPdfVersion(iText.Kernel.Pdf.PdfVersion.PDF_1_4)
                         );
            iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
            pdfDocument.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4);
            var settings = new iText.Html2pdf.ConverterProperties()
                .SetFontProvider(new iText.Html2pdf.Resolver.Font.DefaultFontProvider(false, true, false)
            );
            iText.Html2pdf.HtmlConverter.ConvertToPdf(html, pdfDocument, settings);
            writer.Close();

            if (string.IsNullOrEmpty(configuration["CertChain"])) return pdfStreamEncrypted.ToArray(); // return not signed password protected pdf
            Org.BouncyCastle.Pkcs.Pkcs12Store pk12 = new Org.BouncyCastle.Pkcs.Pkcs12Store(new FileStream(configuration["CertChain"], FileMode.Open, FileAccess.Read), configuration["CertChainPass"].ToCharArray());
            string alias = null;
            foreach (var a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                    break;
            }

            var pk = pk12.GetKey(alias).Key;
            var ce = pk12.GetCertificateChain(alias);
            var chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }

            try
            {
                return Sign(
                    pdfStreamEncrypted.ToArray(),
                    Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                    chain,
                    pk,
                    iText.Signatures.DigestAlgorithms.SHA512,
                    iText.Signatures.PdfSigner.CryptoStandard.CADES,
                    "Covid test",
                    "Pezinok"
                    );
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error while signing the pdf document");
                return pdfStreamEncrypted.ToArray();
            }
        }


        /// <summary>
        /// Creates pdf from registration
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="testingEntity"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public byte[] GenerateRegistrationPDF(Visitor visitor, string testingEntity, string placeAddress, string product)
        {
            var html = GenerateRegistrationHTML(visitor, testingEntity, placeAddress, product);
            using var pdfStreamEncrypted = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(pdfStreamEncrypted,
                            new iText.Kernel.Pdf.WriterProperties()
                                    .SetStandardEncryption(
                                        Encoding.ASCII.GetBytes(visitor.RC),
                                        Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                                        iText.Kernel.Pdf.EncryptionConstants.ALLOW_PRINTING,
                                        iText.Kernel.Pdf.EncryptionConstants.ENCRYPTION_AES_256
                                    )

                         //                                    .SetPdfVersion(iText.Kernel.Pdf.PdfVersion.PDF_1_4)
                         );
            iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
            pdfDocument.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4);
            var settings = new iText.Html2pdf.ConverterProperties()
                .SetFontProvider(new iText.Html2pdf.Resolver.Font.DefaultFontProvider(false, true, false)
            );
            iText.Html2pdf.HtmlConverter.ConvertToPdf(html, pdfDocument, settings);
            writer.Close();

            if (string.IsNullOrEmpty(configuration["CertChain"])) return pdfStreamEncrypted.ToArray(); // return not signed password protected pdf
            Org.BouncyCastle.Pkcs.Pkcs12Store pk12 = new Org.BouncyCastle.Pkcs.Pkcs12Store(new FileStream(configuration["CertChain"], FileMode.Open, FileAccess.Read), configuration["CertChainPass"].ToCharArray());
            string alias = null;
            foreach (var a in pk12.Aliases)
            {
                alias = ((string)a);
                if (pk12.IsKeyEntry(alias))
                    break;
            }

            var pk = pk12.GetKey(alias).Key;
            var ce = pk12.GetCertificateChain(alias);
            var chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
            {
                chain[k] = ce[k].Certificate;
            }

            try
            {
                return Sign(
                    pdfStreamEncrypted.ToArray(),
                    Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                    chain,
                    pk,
                    iText.Signatures.DigestAlgorithms.SHA512,
                    iText.Signatures.PdfSigner.CryptoStandard.CADES,
                    "Covid test",
                    "Pezinok"
                    );
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Error while signing the pdf document");
                return pdfStreamEncrypted.ToArray();
            }
        }

        /// <summary>
        /// Digitaly sign pdf
        /// </summary>
        /// <param name="src"></param>
        /// <param name="pass"></param>
        /// <param name="chain"></param>
        /// <param name="pk"></param>
        /// <param name="digestAlgorithm"></param>
        /// <param name="subfilter"></param>
        /// <param name="reason"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public byte[] Sign(
            byte[] src,
            byte[] pass,
            Org.BouncyCastle.X509.X509Certificate[] chain,
            Org.BouncyCastle.Crypto.ICipherParameters pk,
            String digestAlgorithm,
            iText.Signatures.PdfSigner.CryptoStandard subfilter,
            String reason,
            String location
        )
        {
            using MemoryStream outputMemoryStream = new MemoryStream();
            using MemoryStream memoryStream = new MemoryStream(src);
            using MemoryStream signerMemoryStream = new MemoryStream();

            var readerProperties = new iText.Kernel.Pdf.ReaderProperties();
            readerProperties.SetPassword(pass);

            using iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream, readerProperties);
            iText.Signatures.PdfSigner signer =
                new iText.Signatures.PdfSigner(
                    pdfReader,
                    signerMemoryStream,
                    new iText.Kernel.Pdf.StampingProperties());

            // Create the signature appearance
            iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(350, 100, 200, 100);
            iText.Signatures.PdfSignatureAppearance appearance = signer.GetSignatureAppearance();
            appearance.SetReason(reason)
                .SetLocation(location)
                // Specify if the appearance before field is signed will be used
                // as a background for the signed field. The "false" value is the default value.
                .SetReuseAppearance(false)
                .SetPageRect(rect)
                .SetPageNumber(1);
            signer.SetFieldName("sig");

            iText.Signatures.IExternalSignature pks = new iText.Signatures.PrivateKeySignature(pk, digestAlgorithm);

            // Sign the document using the detached mode, CMS or CAdES equivalent.
            var crlList = new List<iText.Signatures.ICrlClient>();
            signer.SignDetached(pks, chain, crlList, null, null, 0, subfilter);

            pdfReader.Close();
            memoryStream.Close();
            src = outputMemoryStream.ToArray();
            outputMemoryStream.Close();
            return signerMemoryStream.ToArray();
        }


        /// <summary>
        /// Decode visitor data from database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<VerificationData> GetResult(string id)
        {
            logger.LogInformation($"VerificationData loaded from database: {id.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_OBJECTS}", id.ToString());
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
        public virtual async Task<VerificationData> SetResult(Model.VerificationData verificationData, bool mustBeNew)
        {
            if (verificationData is null)
            {
                throw new ArgumentNullException(nameof(verificationData));
            }

            var objectToEncode = Newtonsoft.Json.JsonConvert.SerializeObject(verificationData);
            logger.LogInformation($"Setting verificationData {verificationData.Id.GetHashCode()}");
            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var encoded = aes.EncryptToBase64String(objectToEncode);
            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_OBJECTS}", verificationData.Id, encoded, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception("Error creating record in the database");
            }

            return verificationData;
        }
    }
}
