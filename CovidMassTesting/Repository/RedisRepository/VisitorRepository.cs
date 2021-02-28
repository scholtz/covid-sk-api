using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
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
        private readonly string REDIS_KEY_REGISTATION_OBJECTS = "REGISTRATION";
        private readonly string REDIS_KEY_ID2REGISTATION = "REDIS_KEY_ID2REGISTATION";

        private readonly string REDIS_KEY_VISITORS_OBJECTS = "VISITOR";
        private readonly string REDIS_KEY_RESULTVERIFICATION_OBJECTS = "RESULTS";
        private readonly string REDIS_KEY_RESULTS_NEW_OBJECTS = "RESULTSLIST";
        private readonly string REDIS_KEY_TEST2RESULTS_NEW_OBJECTS = "TEST2RESULTID";
        private readonly string REDIS_KEY_TEST2VISITOR = "TEST2VISITOR";
        private readonly string REDIS_KEY_PERSONAL_NUMBER2VISITOR = "PNUM2VISITOR";
        private readonly string REDIS_KEY_DAY2VISITOR = "DAY2VISITOR";
        private readonly string REDIS_KEY_OPENDAYS = "OPENDAYS";
        private readonly string REDIS_KEY_DOCUMENT_QUEUE = "DOCUMENT_QUEUE";
        private readonly string REDIS_KEY_RESULT_QUEUE = "RESULT_QUEUE";
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
        /// <param name="notify">Send notification</param>
        /// <returns></returns>
        public async Task<Visitor> Add(Visitor visitor, bool notify)
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
                case "foreign":
                    if (!string.IsNullOrEmpty(visitor.Passport))
                    {
                        await MapPersonalNumberToVisitorCode(visitor.Passport, visitor.Id);
                    }
                    break;
                case "idcard":
                case "child":
                default:
                    if (!string.IsNullOrEmpty(visitor.RC))
                    {
                        await MapPersonalNumberToVisitorCode(visitor.RC, visitor.Id);
                    }
                    break;
            }

            var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
            var slot = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
            await MapDayToVisitorCode(slot.TestingDayId, visitor.Id);

            if (notify)
            {
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

                    var pdf = GenerateRegistrationPDF(visitor, pp?.CompanyName, place?.Name, place?.Address, product?.Name);
                    attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                    {
                        Content = Convert.ToBase64String(pdf),
                        Filename = $"reg-{visitor.LastName}{visitor.FirstName}-{slot.TimeInCET.ToString("MMdd")}.pdf",
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
                    new Model.Email.VisitorRegistrationEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                    {
                        Code = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                        Date = $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}",
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
                            $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}",
                            place.Name
                    )));
                }
                CultureInfo.CurrentCulture = oldCulture;
                CultureInfo.CurrentUICulture = oldUICulture;
            }
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
        /// Remove registration
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<bool> RemoveRegistration(string id)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_REGISTATION_OBJECTS}", id);
            return true;
        }
        public virtual async Task<bool> RemoveResult(string id)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}", id);
            return true;
        }
        /// <summary>
        /// Decode visitor data from database
        /// </summary>
        /// <param name="codeInt"></param>
        /// <returns></returns>
        public virtual async Task<Visitor> GetVisitor(int codeInt, bool fixOnLoad = true)
        {
            logger.LogInformation($"Visitor loaded from database: {codeInt.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", codeInt.ToString());
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            var ret = Newtonsoft.Json.JsonConvert.DeserializeObject<Visitor>(decoded);
            if (fixOnLoad)
            {
                return await FixVisitor(ret, true);
            }
            else
            {
                return ret;
            }
        }
        /// <summary>
        /// Loads registrtion.  
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<Registration> GetRegistration(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            logger.LogInformation($"Registration loaded from database: {(configuration["key"] + id).GetSHA256Hash()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_REGISTATION_OBJECTS}", id);
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            using var aes = new Aes(configuration["key"], configuration["iv"]);
            var decoded = aes.DecryptFromBase64String(encoded);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(decoded);
        }
        public virtual Task<Result> GetResultObject(string id)
        {
            return redisCacheClient.Db0.HashGetAsync<Result>($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}", id);
        }
        public virtual async Task<Result> GetResultObjectByTestId(string testId)
        {
            var id = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_TEST2RESULTS_NEW_OBJECTS}", testId);
            return await redisCacheClient.Db0.HashGetAsync<Result>($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}", id);
        }
        /// <summary>
        /// Fills in missing data if possible
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="save">If true, it saves it</param>
        /// <returns></returns>
        protected async Task<Visitor> FixVisitor(Visitor visitor, bool save)
        {
            var updated = false;
            if (string.IsNullOrEmpty(visitor.PersonType))
            {
                visitor.PersonType = "idcard";
            }

            if (string.IsNullOrEmpty(visitor.Address))
            {
                visitor.Address = $"{visitor.Street} {visitor.StreetNo}, {visitor.ZIP} {visitor.City}";
                updated = true;
            }

            if (!string.IsNullOrEmpty(visitor.RC))
            {
                var rc = visitor.RC;
                rc = rc.Replace(" ", "").Replace("/", "");
                if (rc != visitor.RC)
                {
                    visitor.RC = rc;
                    updated = true;
                }
            }

            // Fix years where person put 80 instead of 1980
            if (visitor.BirthDayYear < 1900)
            {
                if ((visitor.PersonType == "idcard" || visitor.PersonType == "child"))
                {
                    if (visitor.RC?.Length == 9 || visitor.RC?.Length == 10)
                    {
                        var year = visitor.RC.Substring(0, 2);
                        if (int.TryParse(year, out var yearInt))
                        {
                            if (yearInt > 21)
                            {
                                yearInt += 1900;
                            }
                            else
                            {
                                yearInt += 2000;
                            }
                            if (visitor.BirthDayYear != yearInt)
                            {
                                visitor.BirthDayYear = yearInt;
                                updated = true;
                            }
                        }
                    }
                    else
                    {
                        visitor.BirthDayYear += 1900;
                        updated = true;
                    }
                }
            }

            if ((visitor.PersonType == "idcard" || visitor.PersonType == "child")
                && !string.IsNullOrEmpty(visitor.RC)
                && (visitor.RC?.Length == 9 || visitor.RC?.Length == 10)
                )
            {

                // fix day
                if (!visitor.BirthDayDay.HasValue)
                {
                    var day = visitor.RC.Substring(4, 2);
                    if (int.TryParse(day, out var dayInt))
                    {
                        if (dayInt >= 1 && dayInt <= 31)
                        {
                            if (visitor.BirthDayDay != dayInt)
                            {
                                visitor.BirthDayDay = dayInt;
                                updated = true;
                            }
                        }
                    }
                }
                // fix month
                if (!visitor.BirthDayMonth.HasValue || string.IsNullOrEmpty(visitor.Gender))
                {
                    var month = visitor.RC.Substring(2, 2);
                    if (int.TryParse(month, out var monthInt))
                    {
                        if (monthInt > 50)
                        {
                            monthInt -= 50;
                            if (visitor.Gender != "F")
                            {
                                visitor.Gender = "F";
                                updated = true;
                            }
                        }
                        else
                        {
                            if (visitor.Gender != "M")
                            {
                                visitor.Gender = "M";
                                updated = true;
                            }
                        }
                        if (monthInt >= 1 && monthInt <= 12)
                        {
                            if (visitor.BirthDayMonth != monthInt)
                            {
                                visitor.BirthDayMonth = monthInt;
                                updated = true;
                            }
                        }
                    }
                }
                // fix year
                var year = visitor.RC.Substring(0, 2);
                if (int.TryParse(year, out var yearInt))
                {
                    if (yearInt > 21)
                    {
                        yearInt += 1900;
                    }
                    else
                    {
                        yearInt += 2000;
                    }
                    if (visitor.BirthDayYear != yearInt)
                    {
                        visitor.BirthDayYear = yearInt;
                        updated = true;
                    }
                }
            }

            if (updated && save)
            {
                logger.LogInformation("Post fix visitor");
                await SetVisitor(visitor, false);
            }
            return visitor;
        }
        public string FormatDocument(string input)
        {
            return input?
                .ToUpper()
                .Replace("‐", "")//utf slash?
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("/", "")
                .Trim();
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
            if (string.IsNullOrEmpty(visitor.Address))
            {
                visitor.Address = $"{visitor.Street} {visitor.StreetNo}, {visitor.ZIP} {visitor.City}";
            }
            visitor = await FixVisitor(visitor, false);
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
        /// Add or update registration
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="mustBeNew"></param>
        /// <returns></returns>
        public virtual async Task<Registration> SetRegistration(Registration registration, bool mustBeNew)
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
            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_REGISTATION_OBJECTS}", registration.Id, encoded, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception("Error creating record in the database");
            }
            if (!string.IsNullOrEmpty(registration.RC))
            {
                logger.LogInformation($"Company: " + $"{configuration["key"]}-{registration.RC}".GetSHA256Hash());
                await MapHashedIdToRegistration($"{configuration["key"]}-{registration.RC}".GetSHA256Hash(), registration.Id);
            }
            if (!string.IsNullOrEmpty(registration.Passport))
            {
                await MapHashedIdToRegistration($"{configuration["key"]}-{registration.Passport}".GetSHA256Hash(), registration.Id);
            }
            foreach (var item in registration.CompanyIdentifiers)
            {
                logger.LogInformation($"Company: {item.CompanyId} {item.EmployeeId}");
                await MapHashedIdToRegistration(MakeCompanyPeronalNumberHash(item.CompanyId, item.EmployeeId), registration.Id);
            }
            return registration;
        }
        /// <summary>
        /// Make hash from company personal number
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public string MakeCompanyPeronalNumberHash(string companyId, string employeeId)
        {
            logger.LogInformation($"MakeCompanyPeronalNumberHash '{companyId}' '{employeeId}'");
            return $"{configuration["key"]}-{companyId}-{employeeId}".GetSHA256Hash();
        }

        public virtual async Task<Result> SetResultObject(Result result, bool mustBeNew)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}", result.Id, result, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception("Error creating record in the database");
            }

            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2RESULTS_NEW_OBJECTS}", result.TestingSetId, result.Id, false);
            return result;
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
        /// Maps testing code to visitor code
        /// </summary>
        /// <param name="hashedId"></param>
        /// <param name="registrationId"></param>
        /// <returns></returns>
        public virtual async Task MapHashedIdToRegistration(string hashedId, string registrationId)
        {
            await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_ID2REGISTATION}", hashedId, registrationId);
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
        /// UnMapHashedIdToRegistration
        /// </summary>
        /// <param name="hashedId"></param>
        /// <returns></returns>
        public virtual async Task UnMapHashedIdToRegistration(string hashedId)
        {
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_ID2REGISTATION}", hashedId);
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
        /// GetRegistrationIdFromHashedId
        /// </summary>
        /// <param name="hashedId"></param>
        /// <returns></returns>
        public virtual Task<string> GetRegistrationIdFromHashedId(string hashedId)
        {
            return redisCacheClient.Db0.HashGetAsync<string>(
                $"{configuration["db-prefix"]}{REDIS_KEY_ID2REGISTATION}",
                hashedId
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
            personalNumber = FormatDocument(personalNumber);
            await redisCacheClient.Db0.HashSetAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash(),
                visitorCode
            );
        }
        /// <summary>
        /// When 
        ///  - visitor registers
        ///  - visitor comes to the place at different day
        /// </summary>
        /// <param name="day"></param>
        /// <param name="visitorCode"></param>
        /// <returns></returns>
        public virtual async Task<bool> MapDayToVisitorCode(long day, int visitorCode)
        {
            await MapDay(day);
            return await redisCacheClient.Db0.HashSetAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_DAY2VISITOR}-{day}",
                Encoding.ASCII.GetBytes($"{day}{visitorCode}{configuration["key"]}").GetSHA256Hash(),
                visitorCode
            );
        }
        public virtual Task<bool> UnMapDayToVisitorCode(long day, int visitorCode)
        {
            return redisCacheClient.Db0.HashDeleteAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_DAY2VISITOR}-{day}",
                Encoding.ASCII.GetBytes($"{day}{visitorCode}{configuration["key"]}").GetSHA256Hash()
            );
        }
        public virtual Task<bool> MapDay(long day)
        {
            return redisCacheClient.Db0.HashSetAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_OPENDAYS}",
                day.ToString(),
                day
            );
        }
        public virtual Task<bool> UnMapDay(long day)
        {
            return redisCacheClient.Db0.HashDeleteAsync(
                $"{configuration["db-prefix"]}{REDIS_KEY_OPENDAYS}",
                day.ToString()
            );
        }

        /// <summary>
        /// Removes personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public virtual async Task UnMapPersonalNumber(string personalNumber)
        {
            personalNumber = FormatDocument(personalNumber);
            await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}", Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash());
        }
        /// <summary>
        /// Returns visitor code from personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        public virtual async Task<int?> GETVisitorCodeFromPersonalNumber(string personalNumber)
        {

            var ret = await redisCacheClient.Db0.HashGetAsync<int?>(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{personalNumber}{configuration["key"]}").GetSHA256Hash()
            );
            if (ret != null)
            {
                return ret;
            }
            var pn = FormatDocument(personalNumber);
            ret = await redisCacheClient.Db0.HashGetAsync<int?>(
                $"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}",
                Encoding.ASCII.GetBytes($"{pn}{configuration["key"]}").GetSHA256Hash()
            );
            return ret;
        }
        /// <summary>
        /// Map visitor code to testing code
        /// </summary>
        /// <param name="codeInt"></param>
        /// <param name="testCodeClear"></param>
        /// <param name="adminWorker"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public async Task<string> ConnectVisitorToTest(int codeInt, string testCodeClear, string adminWorker, string ipAddress)
        {
            var visitorCode = await GETVisitorCodeFromTesting(testCodeClear);
            if (visitorCode.HasValue)
            {
                if (codeInt != visitorCode)
                {
                    throw new Exception("Tento kód testovacej sady je použitý pre iného návštevníka. Zadajte iný prosím.");
                }
            }
            await MapTestingSetToVisitorCode(codeInt, testCodeClear);
            await UpdateTestingState(codeInt, TestResult.TestIsBeingProcessing, testCodeClear, true, adminWorker, ipAddress);
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
            return UpdateTestingState(code, state, "", true, "", "");
        }
        /// <summary>
        /// Updates the visitor test result
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public async Task<bool> UpdateTestWithoutNotification(int code, string state, bool isAdmin)
        {
            var visitor = await GetVisitor(code);
            if (visitor.TestingTime.HasValue)
            {
                var confWait = configuration["minWaitTimeForResultMinutes"] ?? "15";
                if (!isAdmin && state != TestResult.TestMustBeRepeated)
                {
                    if (visitor.TestingTime.Value.AddMinutes(int.Parse(confWait)) > DateTimeOffset.Now)
                    {
                        return false;
                    }
                }
            }
#if SaveASAP
            visitor.Result = state;
            visitor.LastUpdate = DateTimeOffset.Now;
            visitor.TestResultTime = visitor.LastUpdate;
            await SetVisitor(visitor, false);
#endif
            return true;
        }
        /// <summary>
        /// Updates testing state
        /// </summary>
        /// <param name="code"></param>
        /// <param name="state"></param>
        /// <param name="testingSet"></param>
        /// <param name="updateStats"></param>
        /// <param name="adminWorker"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public async Task<bool> UpdateTestingState(int code, string state, string testingSet = "", bool updateStats = true, string adminWorker = "", string ipAddress = "")
        {
            logger.LogInformation($"Updating state for {code.GetHashCode()}");
            var visitor = await GetVisitor(code);
            if (visitor == null)
            {
                throw new Exception(string.Format(localizer[Repository_RedisRepository_VisitorRepository.Visitor_with_code__0__not_found].Value, code));
            }

            if (visitor.Result == state && visitor.ResultNotifiedAt.HasValue)
            {
                // repeated requests should not send emails
                return true;
            }
            var forceSend = false;

            if (visitor.Result == TestResult.PositiveCertificateTaken || visitor.Result == TestResult.PositiveWaitingForCertificate)
            {
                if (state == TestResult.TestMustBeRepeated || state == TestResult.NegativeCertificateTaken || state == TestResult.NegativeWaitingForCertificate)
                {
                    forceSend = true;
                }
            }
            if (visitor.Result == TestResult.NegativeCertificateTaken || visitor.Result == TestResult.NegativeWaitingForCertificate)
            {
                if (state == TestResult.TestMustBeRepeated || state == TestResult.PositiveWaitingForCertificate || state == TestResult.PositiveCertificateTaken)
                {
                    forceSend = true;
                }
            }
            if (state == TestResult.TestMustBeRepeated)
            {
                forceSend = true;
            }

            visitor.Result = state;
            switch (state)
            {
                case TestResult.TestMustBeRepeated:
                    visitor.ResultNotifiedAt = DateTimeOffset.UtcNow;
                    break;
                case TestResult.TestIsBeingProcessing:
                    visitor.TestingTime = DateTimeOffset.UtcNow;
                    visitor.ResultNotifiedCount = null;
                    visitor.ResultNotifiedAt = null;
                    visitor.TestingSet = testingSet;
                    visitor.VerifiedBy = adminWorker;
                    visitor.VerifiedFromIP = ipAddress;
                    if (!string.IsNullOrEmpty(adminWorker))
                    {
                        var user = await userRepository.GetPublicUser(adminWorker);
                        if (!string.IsNullOrEmpty(user.Place) && visitor.ChosenPlaceId != user.Place)
                        {
                            logger.LogInformation($"User has changed place from {visitor.ChosenPlaceId} to {user.Place} {code.GetHashCode()}");
                            visitor.ChosenPlaceId = user.Place;
                        }
                    }

                    break;
            }
            visitor.LastUpdate = DateTimeOffset.Now;
            await SetVisitor(visitor, false);
            var time = DateTimeOffset.Now;
            await MapDayToVisitorCode(new DateTimeOffset(time.Year, time.Month, time.Day, 0, 0, 0, TimeSpan.Zero).Ticks, visitor.Id);
            try
            {
                // update slots stats
                if (updateStats)
                {
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

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(
                            string.Format(Repository_RedisRepository_VisitorRepository.Dear__0___there_were_some_technical_issues_with_your_test__Please_visit_the_sampling_place_again_and_repeat_the_test_procedure__You_can_use_the_same_registration_as_before_,
                            $"{visitor.FirstName} {visitor.LastName}")));

                    }
                    await emailSender.SendEmail(
                        localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                        visitor.Email,
                        $"{visitor.FirstName} {visitor.LastName}",
                        new Model.Email.VisitorTestingToBeRepeatedEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });


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
                        new Model.Email.VisitorTestingInProcessEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                        });

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        /*
                         * send only by email..
                        await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(string.Format(
                            Repository_RedisRepository_VisitorRepository.Dear__0___your_test_is_in_processing__Please_wait_for_further_instructions_in_next_sms_message_,
                            $"{visitor.FirstName} {visitor.LastName}"
                            )));
                        */
                    }
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;
                    break;
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.NegativeWaitingForCertificate:
                    if (forceSend || !visitor.ResultNotifiedAt.HasValue)
                    {
                        await SendResults(visitor);
                    }
                    break;
                case TestResult.PositiveCertificateTaken:
                case TestResult.NegativeCertificateTaken:
                    await SendResults(visitor);
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

            pass = FormatDocument(pass); // normalize

            if (pass.Length < 4)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            var visitor = await GetVisitor(code);
            if (visitor == null)
            {
                throw new Exception("Neznámy kód registrácie");
            }
            if (visitor.PersonType == "foreign")
            {
                var passport = FormatDocument(visitor.Passport);

                if (passport.Length < 4) { throw new Exception("Your passport length is smaller then 4 chars. It is probably error. Contact support please."); }

                if (!visitor.Passport.EndsWith(pass, true, CultureInfo.InvariantCulture))
                {
                    throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
                }
            }
            else
            {
                if (visitor.RC == null || visitor.RC.Length < 4) { throw new Exception("Vaše rodné číslo je kratšie ako 4 znaky. Je to pravdepodobne chyba. Prosím kontaktujte podporu."); }

                if (!visitor.RC.EndsWith(pass, true, CultureInfo.InvariantCulture))
                {
                    throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
                }
            }


            switch (visitor.Result)
            {
                case TestResult.PositiveWaitingForCertificate:
                    visitor.Result = TestResult.PositiveCertificateTaken;
                    break;
                case TestResult.NegativeWaitingForCertificate:
                    visitor.Result = TestResult.NegativeCertificateTaken;
                    break;
            }
            visitor.LastStatusCheck = DateTimeOffset.UtcNow;
            await SetVisitor(visitor, false);
            return new Result { State = visitor.Result, VerificationId = visitor.VerificationId };
        }
        /// <summary>
        /// Generate PDF file with test result
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public async Task<byte[]> GetPublicPDF(int code, string pass)
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
            switch (visitor.Result)
            {
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.PositiveCertificateTaken:
                case TestResult.NegativeWaitingForCertificate:
                case TestResult.NegativeCertificateTaken:
                    // process
                    break;
                default:
                    throw new Exception("Môžeme Vám vygenerovať certifikát iba po absolvovaní testu");
            }

            var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
            //var product = await placeRepository.GetPlaceProduct();
            var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
            var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);
            var oversight = GetOversight(place, visitor.TestingTime);
            return GenerateResultPDF(visitor, pp?.CompanyName, place?.Address, product?.Name, visitor.VerificationId, true, oversight);
        }
        private string GetOversight(Place place, DateTimeOffset? time)
        {
            if (!time.HasValue) return "";
            if (place.MedicalOversight == null) return "";
            var ret = place.MedicalOversight.FirstOrDefault(p => p.From.HasValue && p.From < time && p.Until.HasValue && p.Until > time);
            if (ret != null) return ret.Name;
            ret = place.MedicalOversight.Where(p => p.From.HasValue && !p.Until.HasValue).OrderBy(p => p.From.Value).FirstOrDefault();
            if (ret != null) return ret.Name;
            ret = place.MedicalOversight.FirstOrDefault(p => !p.From.HasValue && !p.Until.HasValue);
            if (ret != null) return ret.Name;
            return "";
        }
        /// <summary>
        /// Generate PDF file with test result
        /// </summary>
        /// <param name="code"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<byte[]> GetResultPDFByEmployee(int code, string user)
        {
            var visitor = await GetVisitor(code);
            if (visitor == null)
            {
                throw new Exception("Skontrolujte prosím správne zadanie kódu registrácie.");
            }
            if (string.IsNullOrEmpty(user))
            {
                throw new Exception("Unauthorized access");
            }
            switch (visitor.Result)
            {
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.PositiveCertificateTaken:
                case TestResult.NegativeWaitingForCertificate:
                case TestResult.NegativeCertificateTaken:
                    // process
                    break;
                default:
                    throw new Exception("Test je v stave: " + visitor.Result);
            }

            var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
            //var product = await placeRepository.GetPlaceProduct();
            var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
            var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);
            var oversight = GetOversight(place, visitor.TestingTime);

            return GenerateResultPDF(visitor, pp?.CompanyName, place?.Address, product?.Name, visitor.VerificationId, false, oversight);
        }

        public async Task<bool> ResendResults(int code, string pass)
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
            switch (visitor.Result)
            {
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.PositiveCertificateTaken:
                case TestResult.NegativeWaitingForCertificate:
                case TestResult.NegativeCertificateTaken:
                    // process
                    break;
                default:
                    throw new Exception("Môžeme Vám vygenerovať certifikát iba po absolvovaní testu");
            }

            var maxResends = configuration["maxResends"] ?? "1";
            var max = int.Parse(maxResends);

            if (visitor.ResultNotifiedCount.HasValue && visitor.ResultNotifiedCount >= max)
            {
                throw new Exception("Bol dosiahnutý limit znovu odoslania certifikátu. Stiahnite si prosím PDF certifikát z webstránky.");
            }

            await SendResults(visitor);

            if (!visitor.ResultNotifiedCount.HasValue)
            {
                visitor.ResultNotifiedCount = 1;
                await SetVisitor(visitor, false);
            }
            else
            {
                visitor.ResultNotifiedCount++;
                await SetVisitor(visitor, false);
            }
            return true;
        }
        private async Task SendResults(Visitor visitor)
        {


            switch (visitor.Result)
            {
                case TestResult.PositiveCertificateTaken:
                case TestResult.NegativeCertificateTaken:
                case TestResult.PositiveWaitingForCertificate:
                case TestResult.NegativeWaitingForCertificate:
                    var oldCulture = CultureInfo.CurrentCulture;
                    var oldUICulture = CultureInfo.CurrentUICulture;
                    var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                    CultureInfo.CurrentCulture = specifiedCulture;
                    CultureInfo.CurrentUICulture = specifiedCulture;
                    var attachments = new List<SendGrid.Helpers.Mail.Attachment>();
                    try
                    {
                        var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                        //var product = await placeRepository.GetPlaceProduct();
                        var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
                        var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);

                        if (!string.IsNullOrEmpty(visitor.VerificationId))
                        {

                            var verification = await GetResultVerification(visitor.VerificationId);
                            var newVerificationState = "";
                            switch (visitor.Result)
                            {

                                case TestResult.PositiveCertificateTaken:
                                case TestResult.PositiveWaitingForCertificate:
                                    if (visitor.Result != TestResult.PositiveWaitingForCertificate)
                                    {
                                        newVerificationState = TestResult.PositiveWaitingForCertificate;
                                    }
                                    break;
                                case TestResult.NegativeCertificateTaken:
                                case TestResult.NegativeWaitingForCertificate:
                                    if (visitor.Result != TestResult.NegativeWaitingForCertificate)
                                    {
                                        newVerificationState = TestResult.NegativeWaitingForCertificate;
                                    }
                                    break;
                            }
                            if (!string.IsNullOrEmpty(newVerificationState) || verification.Time != visitor.TestingTime.Value)
                            {
                                verification.Result = newVerificationState;
                                verification.Time = visitor.TestingTime.Value;
                                await SetResult(verification, false);
                            }
                            var oversight = GetOversight(place, visitor.TestingTime);
                            var pdf = GenerateResultPDF(visitor, pp?.CompanyName, place?.Address, product?.Name, visitor.VerificationId, true, oversight);
                            attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                            {
                                Content = Convert.ToBase64String(pdf),
                                Filename = $"{visitor.LastName}{visitor.FirstName}-{visitor.TestingTime?.ToString("MMdd")}.pdf",
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                        else
                        {
                            var state = TestResult.NotTaken;
                            switch (visitor.Result)
                            {

                                case TestResult.PositiveCertificateTaken:
                                case TestResult.PositiveWaitingForCertificate:
                                    state = TestResult.PositiveWaitingForCertificate;
                                    break;
                                case TestResult.NegativeCertificateTaken:
                                case TestResult.NegativeWaitingForCertificate:
                                    state = TestResult.NegativeWaitingForCertificate;
                                    break;
                            }
                            var result = await SetResult(new VerificationData()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = $"{visitor.FirstName} {visitor.LastName}",
                                Product = product?.Name,
                                TestingAddress = place?.Address,
                                Result = state,
                                TestingEntity = pp?.CompanyName,
                                Time = visitor.TestingTime ?? DateTimeOffset.Now
                            }, true);
                            visitor.VerificationId = result.Id;
                            await SetVisitor(visitor, false);
                            var oversight = GetOversight(place, visitor.TestingTime);

                            var pdf = GenerateResultPDF(visitor, pp?.CompanyName, place?.Address, product?.Name, result.Id, true, oversight);
                            attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                            {
                                Content = Convert.ToBase64String(pdf),
                                Filename = $"{visitor.LastName}{visitor.FirstName}-{visitor.TestingTime?.ToString("MMdd")}.pdf",
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "Error generating file");
                    }

                    await emailSender.SendEmail(
                        localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                        visitor.Email,
                        $"{visitor.FirstName} {visitor.LastName}",
                        new Model.Email.VisitorTestingResultEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                        {
                            Name = $"{visitor.FirstName} {visitor.LastName}",
                            IsSick = visitor.Result == TestResult.PositiveWaitingForCertificate
                        },
                        attachments
                        );
                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {

                        var resultLocalized = "";
                        switch (visitor.Result)
                        {
                            case TestResult.PositiveWaitingForCertificate:
                            case TestResult.PositiveCertificateTaken:
                                resultLocalized = localizer[Repository_RedisRepository_VisitorRepository.POSITIVE];
                                break;
                            case TestResult.NegativeWaitingForCertificate:
                            case TestResult.NegativeCertificateTaken:
                                resultLocalized = localizer[Repository_RedisRepository_VisitorRepository.NEGATIVE];
                                break;
                        }
                        if (!string.IsNullOrEmpty(resultLocalized))
                        {
                            await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(
                                string.Format(
                                    //{0}, {1}, AG test zo dna {2} je {3}. PDF Certifikát získate na: {4}
                                    Repository_RedisRepository_VisitorRepository.Dear__0___your_test_result_has_been_processed__You_can_check_the_result_online__Please_come_to_take_the_certificate_,
                                    $"{visitor.FirstName} {visitor.LastName}",
                                    visitor.BirthDayYear,
                                    visitor.TestingTime?.ToString("dd.MM.yyyy"),
                                    resultLocalized,
                                    configuration["FrontedURL"]
                                    )));
                        }
                    }
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;

                    visitor.ResultNotifiedAt = DateTimeOffset.UtcNow;
                    await SetVisitor(visitor, false);
                    break;
            }
        }
        /// <summary>
        /// When person comes to the queue he can mark him as in the queue
        /// 
        /// It can help other people to check the queue time
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public async Task<bool> Enqueued(int code, string pass)
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
            if (visitor.Result != TestResult.NotTaken)
            {
                return true; // quietly do not accept
            }
            visitor.Enqueued = DateTimeOffset.UtcNow;
            await SetVisitor(visitor, false);
            return true;
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
            var beforeTest = !visitor.TestingTime.HasValue;
            if (visitor.RC?.Length > 4 && !visitor.RC.Trim().EndsWith(pass.Trim(), true, CultureInfo.InvariantCulture))
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            if (visitor.Passport?.Length > 4 && !visitor.Passport.Trim().EndsWith(pass.Trim(), true, CultureInfo.InvariantCulture))
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Invalid_code].Value);
            }
            if (beforeTest)
            {
                if (visitor.Result != TestResult.NotTaken)
                {
                    throw new Exception("Test môže byť zmazaný iba ak ste ešte neprišli na test");
                }
            }
            else
            {
                if (visitor.Result != TestResult.NegativeCertificateTaken && visitor.Result != TestResult.NegativeWaitingForCertificate)
                {
                    throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Personal_data_may_be_deleted_only_after_the_test_has_proven_negative_result_and_person_receives_the_certificate_].Value);
                }

                if (!visitor.TestingTime.HasValue)
                {
                    throw new Exception("S Vašim testom sa vyskytla technická chyba, kontaktujte podporu prosím");
                }

                if (visitor.TestingTime.Value.AddDays(5) > DateTimeOffset.Now)
                {
                    throw new Exception("Test ešte nemôžeme vymazať pretože údaje ešte neboli finálne spracované pre hygienu. Test sa môže zmazať 5 dní od vykonania testu.");
                }
            }
            await Remove(visitor.Id);
            if (!string.IsNullOrEmpty(visitor.TestingSet))
            {
                await UnMapTestingSet(visitor.TestingSet);
            }

            switch (visitor.PersonType)
            {
                case "foreign":
                    if (!string.IsNullOrEmpty(visitor.Passport))
                    {
                        await UnMapPersonalNumber(visitor.Passport);
                    }
                    break;
                case "idcard":
                case "child":
                default:
                    if (!string.IsNullOrEmpty(visitor.RC))
                    {
                        await UnMapPersonalNumber(visitor.RC);
                    }
                    break;
            }

            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;
            if (!string.IsNullOrEmpty(visitor.Email))
            {
                await emailSender.SendEmail(
                    localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                    visitor.Email,
                    $"{visitor.FirstName} {visitor.LastName}",
                    new Model.Email.PersonalDataRemovedEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                    {
                        Name = $"{visitor.FirstName} {visitor.LastName}",
                    });
            }
            if (!string.IsNullOrEmpty(visitor.Phone) && string.IsNullOrEmpty(visitor.Email))
            {
                // send sms only if email is not available
                await smsSender.SendSMS(visitor.Phone,
                    new Model.SMS.Message(string.Format(localizer[Repository_RedisRepository_VisitorRepository.Dear__0__We_have_removed_your_personal_data_from_the_database__Thank_you_for_taking_the_covid_test].Value, $"{visitor.FirstName} {visitor.LastName}"))
                );

            }
            CultureInfo.CurrentCulture = oldCulture;
            CultureInfo.CurrentUICulture = oldUICulture;
            if (beforeTest)
            {
                var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                if (place == null) { throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.We_are_not_able_to_find_chosen_testing_place].Value); }
                var slotM = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
                if (slotM == null) { throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.We_are_not_able_to_find_chosen_5_min__slot].Value); }
                var slotH = await slotRepository.GetHourSlot(visitor.ChosenPlaceId, slotM.HourSlotId);
                if (slotH == null) { throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.We_are_not_able_to_find_chosen_hour_slot].Value); }
                var slotD = await slotRepository.GetDaySlot(visitor.ChosenPlaceId, slotH.DaySlotId);

                await slotRepository.DecrementRegistration5MinSlot(slotM);
                await slotRepository.DecrementRegistrationHourSlot(slotH);
                await slotRepository.DecrementRegistrationDaySlot(slotD);
                await placeRepository.DecrementPlaceRegistrations(visitor.ChosenPlaceId);
            }

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
        public virtual async Task<IEnumerable<string>> ListAllKeys(DateTimeOffset? day = null)
        {
            if (day.HasValue)
            {
                var keys = $"{configuration["db-prefix"]}{REDIS_KEY_DAY2VISITOR}-{day.Value.Ticks}";
                return await redisCacheClient.Db0.HashValuesAsync<string>(keys);
            }
            return await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}");
        }
        /// <summary>
        /// Lists all keys for Registrations
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<string>> ListAllRegistrationKeys()
        {
            return redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_REGISTATION_OBJECTS}");
        }
        public virtual Task<IEnumerable<string>> ListAllKeysResults()
        {
            return redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}");
        }
        public virtual Task<IEnumerable<string>> ListAllTestsKeys()
        {
            return redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2RESULTS_NEW_OBJECTS}");
        }
        /// <summary>
        /// Lists all result keys
        /// </summary>
        /// <returns></returns>
        public virtual Task<IEnumerable<string>> ListAllResultKeys()
        {
            return redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTVERIFICATION_OBJECTS}");
        }
        /// <summary>
        /// Returns visitor by personal number
        /// </summary>
        /// <param name="personalNumber"></param>
        /// <param name="nullOnMissing"></param>
        /// <returns></returns>
        public async Task<Visitor> GetVisitorByPersonalNumber(string personalNumber, bool nullOnMissing = false)
        {
            var code = await GETVisitorCodeFromPersonalNumber(personalNumber);
            if (!code.HasValue)
            {
                if (nullOnMissing) return null;
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.Unknown_personal_number].Value);
            }

            return await GetVisitor(code.Value);
        }
        /// <summary>
        /// Set test result
        /// </summary>
        /// <param name="testCode"></param>
        /// <param name="result"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public async Task<Result> SetTestResult(string testCode, string result, bool isAdmin)
        {
            var visitorCode = await GETVisitorCodeFromTesting(testCode);
            var ret = new Result()
            {
                State = result,
                TestingSetId = testCode
            };
            if (!visitorCode.HasValue)
            {
                ret.Matched = false;
            }
            if (visitorCode.HasValue)
            {
                ret.Matched = true;
                ret.TimeIsValid = await UpdateTestWithoutNotification(visitorCode.Value, result, isAdmin);
            }
            await SetResultObject(ret, false);
            if (ret.TimeIsValid)
            {
                if (configuration["SendResultsThroughQueue"] == "1")
                {
                    await AddToResultQueue(ret.Id);
                }
                else
                {
                    logger.LogInformation($"SendResultsThroughQueue 0: processing {result}");
                    await UpdateTestingState(visitorCode.Value, result);
                }
            }
            return ret;
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

        public virtual async Task<bool> AddToResultQueue(string resultId)
        {
            return await redisCacheClient.Db0.ListAddToLeftAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}", resultId) > 0;
            //return redisCacheClient.Db0.SortedSetAddAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}", resultId, DateTimeOffset.UtcNow.Ticks);
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
        public virtual async Task<string> PopFromResultQueue()
        {
            return await redisCacheClient.Db0.ListGetFromRightAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}");

            /*
            var item = await GetFirstItemFromResultQueue();
            await redisCacheClient.Db0.SortedSetRemoveAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}", item);
            return item;
            /**/
        }
        /// <summary>
        /// Process pdf and sms sending after the test
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ProcessSingle()
        {
            var msg = await PopFromResultQueue();
            try
            {
                if (string.IsNullOrEmpty(msg))
                {
                    return false;
                }

                var obj = await GetResultObject(msg);
                if (obj == null)
                {
                    logger.LogError("Result with id {msg} not found");
                    return false;
                }
                // if time is less then 5 minutes from the click allow to change result without notification

                var confWait = configuration["minWaitTimeForFinalResultsMinutes"] ?? "5";
                var waitInt = int.Parse(confWait);
                var delay = obj.Time.AddMinutes(waitInt) - DateTimeOffset.Now;


                var random = new Random();
                var randDelay = TimeSpan.FromMilliseconds(random.Next(100, 1000));
                await Task.Delay(randDelay);

                if (delay > TimeSpan.Zero)
                {
                    await AddToResultQueue(msg); // put at the end of the queue .. in case we close this app we cannot loose the data
                    logger.LogInformation($"Waiting {delay} for next task");
                    await Task.Delay(delay);
                    return true;
                }
                obj = await GetResultObject(msg);
                var latestResult = await GetResultObjectByTestId(obj.TestingSetId);

                var visitorCode = await GETVisitorCodeFromTesting(obj.TestingSetId);
                if (visitorCode.HasValue)
                {
                    logger.LogInformation($"SendResults: processing {latestResult.State}");
                    await UpdateTestingState(visitorCode.Value, latestResult.State);
                }
                else
                {
                    // put the message to the trash
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "!!!!!Error while processing line: " + exc.Message);
                try
                {
                    await AddToResultQueue(msg);
                }
                catch (Exception exc2)
                {
                    logger.LogError(exc2, "!!!!!Error adding back to queue: " + exc2.Message);
                }
                await Task.Delay(10000);
            }
            return true;
        }
        /// <summary>
        /// Removes document from queue and sets the test as taken
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public async Task<bool> RemoveFromDocQueueAndSetTestStateAsTaken(string testId, bool isAdmin)
        {
            var first = await GetFirstItemFromQueue();
            if (first != testId)
            {
                throw new Exception(localizer[Repository_RedisRepository_VisitorRepository.You_can_remove_only_first_item_from_the_queue].Value);
            }

            var visitorCode = await GETVisitorCodeFromTesting(testId);
            if (!visitorCode.HasValue)
            {
                throw new Exception(string.Format(localizer[Repository_RedisRepository_VisitorRepository.Visitor_with_code__0__not_found].Value, visitorCode));
            }

            var visitor = await GetVisitor(visitorCode.Value);
            switch (visitor.Result)
            {
                case TestResult.NegativeWaitingForCertificate:
                    await SetTestResult(testId, TestResult.NegativeCertificateTaken, isAdmin);
                    break;
                case TestResult.PositiveWaitingForCertificate:
                    await SetTestResult(testId, TestResult.PositiveCertificateTaken, isAdmin);
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
        public virtual async Task<string> GetFirstItemFromResultQueue()
        {
            return (await redisCacheClient.Db0.SortedSetRangeByScoreAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}", take: 1, skip: 0)).FirstOrDefault();
        }
        /// <summary>
        /// Fetch next test
        /// </summary>
        /// <returns></returns>
        public async Task<Visitor> GetNextTest()
        {
            var firstTest = await GetFirstItemFromQueue();
            if (firstTest == null)
            {
                return null;
            }

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
        /// <param name="notify"></param>
        /// <returns></returns>
        public async Task<Visitor> Register(Visitor visitor, string managerEmail, bool notify)
        {
            if (visitor is null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            if (!string.IsNullOrEmpty(managerEmail))
            {
                // register to manager place and nearest slot

                var manager = await userRepository.GetPublicUser(managerEmail);
                if (manager == null) throw new Exception($"Manager not found by email {managerEmail}");
                if (notify) // if not notify do not update place by manager nor update the slot
                {
                    visitor.ChosenPlaceId = manager.Place;
                    if (string.IsNullOrEmpty(visitor.ChosenPlaceId))
                    {
                        throw new Exception("Vyberte si najskôr miesto kde sa nachádzate.");
                    }

                    var currentSlot = await slotRepository.GetCurrentSlot(manager.Place, DateTimeOffset.UtcNow);
                    if (currentSlot == null)
                    {
                        throw new Exception("Unable to select testing slot.");
                    }

                    visitor.ChosenSlot = currentSlot.SlotId;
                }
            }
            visitor = await FixVisitor(visitor, false);
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
            if (string.IsNullOrEmpty(managerEmail))
            {
                // manager is not affected by limits

                var LimitPer5MinSlot = place.LimitPer5MinSlot;
                if (!string.IsNullOrEmpty(configuration["LimitPer5MinSlot"]))
                {
                    if (int.TryParse(configuration["LimitPer5MinSlot"], out var confLimit))
                    {
                        if (LimitPer5MinSlot > confLimit)
                        {
                            LimitPer5MinSlot = confLimit;
                        }
                    }
                }


                if (slotM.Registrations >= LimitPer5MinSlot)
                {
                    throw new Exception(
                        string.Format(
                                localizer[Repository_RedisRepository_VisitorRepository.This_5_minute_time_slot_has_reached_the_capacity_].Value,
                                LimitPer5MinSlot
                            )
                        );
                }
                var LimitPer1HourSlot = place.LimitPer1HourSlot;
                if (!string.IsNullOrEmpty(configuration["LimitPer1HourSlot"]))
                {
                    if (int.TryParse(configuration["LimitPer1HourSlot"], out var confLimit))
                    {
                        if (LimitPer1HourSlot > confLimit)
                        {
                            LimitPer1HourSlot = confLimit;
                        }
                    }
                }
                if (place.OtherLimitations != null)
                {
                    foreach (var limit in place.OtherLimitations.Where(l => l.From.Ticks <= slotH.SlotId && l.Until.Ticks > slotH.SlotId))
                    {
                        if (limit.HourLimit < LimitPer1HourSlot)
                        {
                            LimitPer1HourSlot = limit.HourLimit;
                        }
                    }
                }
                if (slotH.Registrations >= LimitPer1HourSlot)
                {
                    throw new Exception(
                        string.Format(
                            localizer[Repository_RedisRepository_VisitorRepository.This_1_hour_time_slot_has_reached_the_capacity_].Value,
                            LimitPer1HourSlot
                        ));
                }
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

                var ret = await Add(visitor, notify);

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
                if (string.IsNullOrEmpty(managerEmail))
                {
                    if (previous.TestingTime.HasValue)
                    {
                        // self registration after has been tested
                        if (previous.Result == TestResult.PositiveCertificateTaken ||
                            previous.Result == TestResult.PositiveWaitingForCertificate
                            )
                        {
                            throw new Exception("Prosím, zostaňte doma. Váš predchádzajúci test preukázal prítomnosť covidu.");
                        }

                        if (previous.TestingTime.Value.AddDays(2) > DateTimeOffset.Now)
                        {
                            throw new Exception("Na test sa môžete zaregistrovať najskôr za 2 dni");
                        }
                        else
                        {
                            // new registration
                            logger.LogInformation($"New updated registration");

                            var ret2 = await Add(visitor, notify);

                            await slotRepository.IncrementRegistration5MinSlot(slotM);
                            await slotRepository.IncrementRegistrationHourSlot(slotH);
                            await slotRepository.IncrementRegistrationDaySlot(slotD);
                            await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);

                            logger.LogInformation($"Incremented: M-{slotM.SlotId}, {slotH.SlotId}, {slotD.SlotId}");

                            return ret2;
                        }
                    }
                    else
                    {
                        // self registration when has not yet been tested

                        visitor.Id = previous.Id; // bar code does not change on new registration with the same personal number

                        // Update registration
                    }
                }
                else
                {
                    if (previous.TestingTime.HasValue)
                    {
                        // manager registration after has been tested
                        if (previous.Result == TestResult.PositiveCertificateTaken ||
                            previous.Result == TestResult.PositiveWaitingForCertificate
                            )
                        {

                            if (previous.TestingTime.Value.AddDays(2) > DateTimeOffset.Now)
                            {
                                throw new Exception("POZOR !! Osoba je pozitívne testovaná v predchádzajúcich 2 dňoch");
                            }
                        }

                        // new registration
                        logger.LogInformation($"New updated registration");

                        var ret2 = await Add(visitor, notify);

                        await slotRepository.IncrementRegistration5MinSlot(slotM);
                        await slotRepository.IncrementRegistrationHourSlot(slotH);
                        await slotRepository.IncrementRegistrationDaySlot(slotD);
                        await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);

                        logger.LogInformation($"Incremented: M-{slotM.SlotId}, {slotH.SlotId}, {slotD.SlotId}");

                        return ret2;
                    }
                    else
                    {
                        // manager registration when has not yet been tested

                        visitor.Id = previous.Id; // bar code does not change on new registration with the same personal number

                        // Update registration
                    }
                }
                var slot = slotM;
                if (string.IsNullOrEmpty(managerEmail)) // do not update the manager language
                {
                    visitor.Language = CultureInfo.CurrentCulture.Name;
                }
                else
                {
                    if (string.IsNullOrEmpty(visitor.Language)) visitor.Language = CultureInfo.CurrentCulture.Name;
                }
                var ret = await SetVisitor(visitor, false);
                if (previous.ChosenPlaceId != visitor.ChosenPlaceId)
                {
                    await placeRepository.DecrementPlaceRegistrations(previous.ChosenPlaceId);
                    await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);
                }
                var code = visitor.Id.ToString();
                var codeFormatted = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}";


                if (string.IsNullOrEmpty(managerEmail))
                {
                    var oldCulture = CultureInfo.CurrentCulture;
                    var oldUICulture = CultureInfo.CurrentUICulture;
                    var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                    CultureInfo.CurrentCulture = specifiedCulture;
                    CultureInfo.CurrentUICulture = specifiedCulture;
                    // send EMAIL/SMS notifications only if user registers himself

                    var attachments = new List<SendGrid.Helpers.Mail.Attachment>();

                    try
                    {
                        //var product = await placeRepository.GetPlaceProduct();
                        var pp = await placeProviderRepository.GetPlaceProvider(place?.PlaceProviderId);
                        var product = pp.Products.FirstOrDefault(p => p.Id == visitor.Product);

                        var pdf = GenerateRegistrationPDF(visitor, pp?.CompanyName, place?.Name, place?.Address, product?.Name);
                        attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                        {
                            Content = Convert.ToBase64String(pdf),
                            Filename = $"reg-{visitor.LastName}{visitor.FirstName}-{slotD.TimeInCET.ToString("MMdd")}.pdf",
                            Type = "application/pdf",
                            Disposition = "attachment"
                        });
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "Error generating file");
                    }

                    if (notify)
                    {
                        await emailSender.SendEmail(
                            localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                            visitor.Email,
                            $"{visitor.FirstName} {visitor.LastName}",
                            new Model.Email.VisitorChangeRegistrationEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                            {
                                Code = codeFormatted,
                                Name = $"{visitor.FirstName} {visitor.LastName}",
                                Date = $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}",
                                Place = place.Name,
                                PlaceDescription = place.Description

                            }, attachments);

                        if (!string.IsNullOrEmpty(visitor.Phone))
                        {

                            await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message(
                                string.Format(localizer[Repository_RedisRepository_VisitorRepository.Dear__0___we_have_updated_your_registration__1___Time___2___Place___3_].Value,
                                $"{visitor.FirstName} {visitor.LastName}",
                                codeFormatted,
                                $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}",
                                place.Name
                            )));

                        }
                    }
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;
                }

                if (previous.ChosenSlot != visitor.ChosenSlot || previous.ChosenPlaceId != visitor.ChosenPlaceId)
                {
                    try
                    {

                        var slotMPrev = await slotRepository.Get5MinSlot(previous.ChosenPlaceId, previous.ChosenSlot);
                        if (slotMPrev != null)
                        {
                            var slotHPrev = await slotRepository.GetHourSlot(previous.ChosenPlaceId, slotMPrev.HourSlotId);
                            if (slotHPrev != null)
                            {
                                var slotDPrev = await slotRepository.GetDaySlot(previous.ChosenPlaceId, slotHPrev.DaySlotId);
                                if (slotDPrev != null)
                                {
                                    await slotRepository.DecrementRegistration5MinSlot(slotMPrev);
                                    await slotRepository.DecrementRegistrationHourSlot(slotHPrev);
                                    await slotRepository.DecrementRegistrationDaySlot(slotDPrev);

                                    await UnMapDayToVisitorCode(slotDPrev.SlotId, visitor.Id);


                                    logger.LogInformation($"Decremented: M-{slotMPrev.SlotId}, {slotHPrev.SlotId}, {slotDPrev.SlotId}");
                                }
                                else
                                {
                                    logger.LogError($"D Slot does not exists: {previous.ChosenPlaceId}, {slotHPrev.DaySlotId}");
                                }
                            }
                            else
                            {
                                logger.LogError($"H Slot does not exists: {previous.ChosenPlaceId}, {slotMPrev.HourSlotId}");
                            }
                        }
                        else
                        {
                            logger.LogError($"M Slot does not exists: {previous.ChosenPlaceId}, {previous.ChosenSlot}");
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, exc.Message);
                    }
                    await slotRepository.IncrementRegistration5MinSlot(slotM);
                    await slotRepository.IncrementRegistrationHourSlot(slotH);
                    await slotRepository.IncrementRegistrationDaySlot(slotD);
                    await MapDayToVisitorCode(slotD.SlotId, visitor.Id);

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
            if (number == null)
            {
                number = "";
            }

            number = number.Replace(" ", "");
            number = number.Replace("\t", "");
            if (number.StartsWith("00", true, CultureInfo.InvariantCulture))
            {
                number = "+" + number[2..];
            }

            if (number.StartsWith("0", true, CultureInfo.InvariantCulture))
            {
                number = "+421" + number[1..];
            }

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
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_REGISTATION_OBJECTS}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_REGISTATION_OBJECTS}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTS_NEW_OBJECTS}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2RESULTS_NEW_OBJECTS}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2RESULTS_NEW_OBJECTS}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_TEST2VISITOR}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_ID2REGISTATION}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_ID2REGISTATION}", item);
            }
            foreach (var item in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}"))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_PERSONAL_NUMBER2VISITOR}", item);
            }
            foreach (var dayStr in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_OPENDAYS}"))
            {
                foreach (var day2visitorKey in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_DAY2VISITOR}-{dayStr}"))
                {
                    await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_DAY2VISITOR}-{dayStr}", day2visitorKey);
                }
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_OPENDAYS}", dayStr);
            }
            ret += (int)await redisCacheClient.Db0.SetRemoveAllAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}");
            ret += (int)await redisCacheClient.Db0.SetRemoveAllAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}");
            var bool1 = await redisCacheClient.Db0.RemoveAsync($"{configuration["db-prefix"]}{REDIS_KEY_DOCUMENT_QUEUE}");
            var bool2 = await redisCacheClient.Db0.RemoveAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULT_QUEUE}");
            // REDIS_KEY_RESULTS_OBJECTS are intended to stay even if all data has been removed

            return ret;
        }
        /// <summary>
        /// List Sick Visitors. Data Exporter person at the end of testing can fetch all info and deliver them to medical office
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListSickVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ListSickVisitors {from} {count}");
            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(visitor.TestingSet))
                    {

                        if (day.HasValue && visitor.TestingTime.HasValue)
                        {
                            if (visitor.TestingTime < day.Value || visitor.TestingTime > day.Value.AddDays(1))
                            {
                                // export only visitors at specified day
                                continue;
                            }
                        }
                        if (visitor.Result != TestResult.PositiveCertificateTaken && visitor.Result != TestResult.PositiveWaitingForCertificate)
                        {
                            continue;
                        }
                        ret.Add(visitor);
                    }
                }
            }
            logger.LogInformation($"ListSickVisitors {from} {count} END - {ret.Count}");
            return ret;
        }
        /// <summary>
        /// List visitors who has passed the test
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListTestedVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ListTestedVisitors {from} {count}");
            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(visitor.TestingSet))
                    {

                        if (day.HasValue && visitor.TestingTime.HasValue)
                        {
                            if (visitor.TestingTime < day.Value || visitor.TestingTime > day.Value.AddDays(1))
                            {
                                // export only visitors at specified day
                                continue;
                            }
                        }

                        ret.Add(visitor);
                    }
                }
            }
            logger.LogInformation($"ListTestedVisitors {from} {count} END - {ret.Count}");
            return ret;
        }
        /// <summary>
        /// List visitors who has passed the test
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<VisitorAnonymized>> ListAnonymizedVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ListAnonymizedVisitors {from} {count}");
            var ret = new List<VisitorAnonymized>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }
                    string key = configuration["key"];
                    if (!string.IsNullOrEmpty(configuration["AnonymizerKey"]))
                    {
                        key = configuration["AnonymizerKey"];
                    }
                    ret.Add(new VisitorAnonymized(visitor, key));
                }
            }
            logger.LogInformation($"ListAnonymizedVisitors {from} {count} END - {ret.Count}");
            return ret;
        }
        /// <summary>
        /// ListExportableDays
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IEnumerable<DateTimeOffset>> ListExportableDays()
        {
            var ret = new List<DateTimeOffset>();
            try
            {
                foreach (var dayStr in await redisCacheClient.Db0.HashKeysAsync($"{configuration["db-prefix"]}{REDIS_KEY_OPENDAYS}"))
                {
                    ret.Add(new DateTimeOffset(long.Parse(dayStr), TimeSpan.Zero));
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "ListExportableDays error");
            }
            return ret.OrderBy(d => d.Ticks);
        }
        /// <summary>
        /// Export for institution that pays for the tests
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <param name="filterPlaces"></param>
        /// <returns></returns>
        public async Task<IEnumerable<VisitorSimplified>> ProofOfWorkExport(DateTimeOffset? day = null, int from = 0, int count = 9999999, HashSet<string> filterPlaces = null)
        {
            if (filterPlaces is null)
            {
                throw new ArgumentNullException(nameof(filterPlaces));
            }

            logger.LogInformation($"ProofOfWorkExport {from} {count}");
            var ret = new List<VisitorSimplified>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (!filterPlaces.Contains(visitor.ChosenPlaceId))
                    {
                        continue;
                    }

                    if (visitor.TestingTime.HasValue && visitor.TestingTime.Value > DateTimeOffset.MinValue)
                    {
                        if (day.HasValue)
                        {
                            if (visitor.TestingTime < day.Value || visitor.TestingTime > day.Value.AddDays(1))
                            {
                                // export only visitors at specified day
                                continue;
                            }
                        }

                        var rc = visitor.RC;
                        if (visitor.PersonType == "foreign")
                        {
                            rc = visitor.Passport;
                        }

                        var result = "";
                        if (visitor.Result == TestResult.NegativeCertificateTaken ||
                            visitor.Result == TestResult.NegativeWaitingForCertificate
                            )
                        {
                            result = "negatívny";
                        }

                        if (visitor.Result == TestResult.PositiveWaitingForCertificate ||
                            visitor.Result == TestResult.PositiveCertificateTaken
                            )
                        {
                            result = "pozitívny";
                        }
                        if (visitor.Result == TestResult.TestMustBeRepeated
                            )
                        {
                            result = "zneplatnený";
                        }

                        ret.Add(new VisitorSimplified()
                        {
                            TypVysetrenia = "AG test",
                            Meno = visitor.FirstName,
                            Priezvisko = visitor.LastName,
                            RodneCislo = rc,
                            Telefon = visitor.Phone,
                            Mail = visitor.Email,
                            PSC = visitor.ZIP,
                            Mesto = visitor.City,
                            Ulica = visitor.Street,
                            Cislo = visitor.StreetNo,
                            Miesto = visitor.ChosenPlaceId,
                            DatumVysetrenia = visitor.TestingTime?.ToString("yyyy-MM-dd"),
                            VysledokVysetrenia = result
                        });
                    }
                }
            }
            logger.LogInformation($"ProofOfWorkExport {from} {count} END - {ret.Count}");
            return ret;
        }
        /// <summary>
        /// Export company registrations
        /// </summary>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Registration>> ExportRegistrations(int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ExportRegistrations {from} {count}");
            var ret = new List<Registration>();
            foreach (var regId in (await ListAllRegistrationKeys()).OrderBy(i => i).Skip(from).Take(count))
            {
                ret.Add(await GetRegistration(regId));
            }
            logger.LogInformation($"ExportRegistrations {from} {count} END - {ret.Count}");
            return ret;
        }
        /// <summary>
        /// List Sick Visitors. Data Exporter person at the end of testing can fetch all info and deliver them to medical office
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListVisitorsInProcess(DateTimeOffset? day = null, int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ListVisitorsInProcess {from} {count}");

            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (visitor.Result == TestResult.TestIsBeingProcessing)
                    {
                        ret.Add(visitor);
                    }
                }
            }
            logger.LogInformation($"ListVisitorsInProcess {from} {count} END - {ret.Count}");

            return ret;
        }
        /// <summary>
        /// List Sick Visitors. Data Exporter person at the end of testing can fetch all info and deliver them to medical office
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListAllVisitorsWhoDidNotCome(DateTimeOffset? day = null, int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ListAllVisitorsWhoDidNotCome {from} {count}");

            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(visitor.TestingSet)
                        //&& visitor.ChosenSlot < DateTimeOffset.UtcNow.Ticks
                        )
                    {
                        ret.Add(visitor);
                    }
                }
            }
            logger.LogInformation($"ListAllVisitorsWhoDidNotCome {from} {count} END - {ret.Count}");

            return ret;
        }
        /// <summary>
        /// All visitors
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListAllVisitors(DateTimeOffset? day = null, int from = 0, int count = 9999999)
        {
            logger.LogInformation($"ListAllVisitors {from} {count}");

            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys(day)).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    ret.Add(visitor);
                }
            }
            logger.LogInformation($"ListAllVisitors {from} {count} END - {ret.Count}");

            return ret;
        }
        /// <summary>
        /// Visitors at place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="fromRegTime"></param>
        /// <param name="untilRegTime"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Visitor>> ListAllVisitorsAtPlace(
            string placeId,
            DateTimeOffset fromRegTime,
            DateTimeOffset untilRegTime,
            int from = 0,
            int count = 9999999
            )
        {
            logger.LogInformation($"ListAllVisitorsAtPlace {from} {count} {placeId} {fromRegTime.ToString("R")} {untilRegTime.ToString("R")}");

            var ret = new List<Visitor>();
            foreach (var visitorId in (await ListAllKeys()).OrderBy(i => i).Skip(from).Take(count))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (visitor.ChosenPlaceId == placeId
                        && visitor.ChosenSlot >= fromRegTime.ToUniversalTime().Ticks
                        && visitor.ChosenSlot < untilRegTime.ToUniversalTime().Ticks
                        )
                    {
                        ret.Add(visitor);
                    }
                }
            }
            logger.LogInformation($"ListAllVisitorsAtPlace {from} {count} END - {ret.Count}");

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
            if (toSave != ret)
            {
                throw new Exception("Storage does not work");
            }

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
        public string GenerateResultHTML(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid, string oversight)
        {
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;

            var data = new Model.Mustache.TestResult();
            switch (visitor.Result)
            {
                case TestResult.PositiveCertificateTaken:
                case TestResult.PositiveWaitingForCertificate:
                    data.Text = "Pozitívny";
                    data.Description = "Zostaňte prosím v karanténe minimálne 14 dní. Potom si vykonajte ďalší antigénový alebo PCR test aby ste mali istotu že vírus nebudete šíriť medzi ľudí.";
                    break;
                case TestResult.NegativeCertificateTaken:
                case TestResult.NegativeWaitingForCertificate:
                    data.Text = "Negatívny";
                    data.Description = "Aj keď test u Vás nepreukázal COVID, prosím zostaňte ostražitý. V prípade príznakov ako kašeľ, zvýšená teplota, alebo bolesť hlavy choďte prosím na ďalší test.";
                    break;
                default:
                    throw new Exception("Invalid state for PDF generation");
            }

            data.Name = $"{visitor.FirstName} {visitor.LastName}";

            data.BirthDayDay = visitor.BirthDayDay;
            data.BirthDayMonth = visitor.BirthDayMonth;
            data.BirthDayYear = visitor.BirthDayYear;
            if (!string.IsNullOrEmpty(configuration["SignaturePicture"]))
            {
                if (File.Exists(configuration["SignaturePicture"]))
                {
                    data.Signature = Convert.ToBase64String(File.ReadAllBytes(configuration["SignaturePicture"])).Replace("\n", "");
                }
            }

            if (visitor.TestingTime.HasValue)
            {
                data.Date = visitor.TestingTime.Value.ToOffset(new TimeSpan(1, 0, 0)).ToString("f");
            }
            switch (visitor.PersonType)
            {
                case "foreign":
                    data.PassportNumber = visitor.Passport;
                    break;
                case "idcard":
                case "child":
                default:
                    data.PersonalNumber = visitor.RC;
                    break;
            }

            data.TestingAddress = placeAddress;
            data.TestingEntity = testingEntity;
            data.FrontedURL = configuration["FrontedURL"];
            data.ResultGUID = resultguid;
            data.VerifyURL = $"{configuration["FrontedURL"]}#/check/{data.ResultGUID}";
            data.Product = product;
            data.Oversight = oversight;
            var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data.VerifyURL, QRCoder.QRCodeGenerator.ECCLevel.H);
            var qrCode = new QRCoder.QRCode(qrCodeData);
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
        public string GenerateRegistrationHTML(Visitor visitor, string testingEntity, string placeName, string placeAddress, string product)
        {
            var oldCulture = CultureInfo.CurrentCulture;
            var oldUICulture = CultureInfo.CurrentUICulture;
            var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
            CultureInfo.CurrentCulture = specifiedCulture;
            CultureInfo.CurrentUICulture = specifiedCulture;

            var data = new Model.Mustache.TestRegistration
            {
                Name = $"{visitor.FirstName} {visitor.LastName}"
            };

            var slot = slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot).Result;

            data.Date = $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}";

            switch (visitor.PersonType)
            {
                case "foreign":
                    data.PassportNumber = visitor.Passport;
                    break;
                case "idcard":
                case "child":
                default:
                    data.PersonalNumber = visitor.RC;
                    break;
            }

            data.TestingName = placeName;
            data.TestingAddress = placeAddress;
            data.TestingEntity = testingEntity;
            data.FrontedURL = configuration["FrontedURL"];
            data.Product = product;
            data.BirthDayDay = visitor.BirthDayDay;
            data.BirthDayMonth = visitor.BirthDayMonth;
            data.BirthDayYear = visitor.BirthDayYear;

            var b = new BarcodeLib.Barcode();
            var formatted = visitor.Id.ToString();
            if (formatted.Length == 9)
            {
                formatted = formatted.Substring(0, 3) + "-" + formatted.Substring(3, 3) + "-" + formatted.Substring(6, 3);
            }
            data.RegistrationCode = formatted;

            var img = b.Encode(BarcodeLib.TYPE.CODE39, formatted, Color.Black, Color.White, 300, 120);
            using var outDataBar = new MemoryStream();
            img.Save(outDataBar, System.Drawing.Imaging.ImageFormat.Png);
            var barBytes = outDataBar.ToArray();
            data.BarCode = Convert.ToBase64String(barBytes).Replace("\n", "");

            var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(visitor.Id.ToString(), QRCoder.QRCodeGenerator.ECCLevel.H);
            var qrCode = new QRCoder.QRCode(qrCodeData);
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
        /// <param name="sign"></param>
        /// <param name="oversight"></param>
        /// <returns></returns>
        public byte[] GenerateResultPDF(Visitor visitor, string testingEntity, string placeAddress, string product, string resultguid, bool sign = true, string oversight = "")
        {
            string password;

            switch (visitor.PersonType)
            {
                case "foreign":
                    password = visitor.Passport;
                    break;
                case "idcard":
                case "child":
                default:
                    password = visitor.RC?.Replace("/", "").Trim();
                    break;
            }

            var html = GenerateResultHTML(visitor, testingEntity, placeAddress, product, resultguid, oversight);
            using var pdfStreamEncrypted = new MemoryStream();
            iText.Kernel.Pdf.PdfWriter writer;
            if (sign)
            {
                writer = new iText.Kernel.Pdf.PdfWriter(pdfStreamEncrypted,
                                new iText.Kernel.Pdf.WriterProperties()
                                        .SetStandardEncryption(
                                            Encoding.ASCII.GetBytes(password),
                                            Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                                            iText.Kernel.Pdf.EncryptionConstants.ALLOW_PRINTING,
                                            iText.Kernel.Pdf.EncryptionConstants.ENCRYPTION_AES_256
                                        )

                             //                                    .SetPdfVersion(iText.Kernel.Pdf.PdfVersion.PDF_1_4)
                             );
            }
            else
            {
                writer = new iText.Kernel.Pdf.PdfWriter(pdfStreamEncrypted);
            }
            var pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
            pdfDocument.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4);
            var settings = new iText.Html2pdf.ConverterProperties()
                .SetFontProvider(new iText.Html2pdf.Resolver.Font.DefaultFontProvider(false, true, false)
            );
            iText.Html2pdf.HtmlConverter.ConvertToPdf(html, pdfDocument, settings);
            writer.Close();
            if (!sign)
            {
                return pdfStreamEncrypted.ToArray();
            }
            if (string.IsNullOrEmpty(configuration["CertChain"]))
            {
                return pdfStreamEncrypted.ToArray(); // return not signed password protected pdf
            }
            //var pages = pdfDocument.GetNumberOfPages();
            try
            {
                var pk12 = new Org.BouncyCastle.Pkcs.Pkcs12Store(new FileStream(configuration["CertChain"], FileMode.Open, FileAccess.Read), configuration["CertChainPass"].ToCharArray());
                string alias = null;
                foreach (var a in pk12.Aliases)
                {
                    alias = ((string)a);
                    if (pk12.IsKeyEntry(alias))
                    {
                        break;
                    }
                }

                var pk = pk12.GetKey(alias).Key;
                var ce = pk12.GetCertificateChain(alias);
                var chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
                for (var k = 0; k < ce.Length; ++k)
                {
                    chain[k] = ce[k].Certificate;
                }

                return Sign(
                    pdfStreamEncrypted.ToArray(),
                    Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                    chain,
                    pk,
                    iText.Signatures.DigestAlgorithms.SHA512,
                    iText.Signatures.PdfSigner.CryptoStandard.CADES,
                    "Covid test",
                    configuration["SignaturePlace"],
                    2
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
        /// <param name="placeName"></param>
        /// <param name="placeAddress"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        public byte[] GenerateRegistrationPDF(Visitor visitor, string testingEntity, string placeName, string placeAddress, string product)
        {
            var password = "";

            switch (visitor.PersonType)
            {
                case "foreign":
                    password = visitor.Passport;
                    break;
                case "idcard":
                case "child":
                default:
                    password = visitor.RC?.Replace("/", "").Trim();
                    break;
            }
            var html = GenerateRegistrationHTML(visitor, testingEntity, placeName, placeAddress, product);
            using var pdfStreamEncrypted = new MemoryStream();
            var writer = new iText.Kernel.Pdf.PdfWriter(pdfStreamEncrypted,
                            new iText.Kernel.Pdf.WriterProperties()
                                    .SetStandardEncryption(
                                        Encoding.ASCII.GetBytes(password),
                                        Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                                        iText.Kernel.Pdf.EncryptionConstants.ALLOW_PRINTING,
                                        iText.Kernel.Pdf.EncryptionConstants.ENCRYPTION_AES_256
                                    )

                         //                                    .SetPdfVersion(iText.Kernel.Pdf.PdfVersion.PDF_1_4)
                         );
            var pdfDocument = new iText.Kernel.Pdf.PdfDocument(writer);
            pdfDocument.SetDefaultPageSize(iText.Kernel.Geom.PageSize.A4);
            var settings = new iText.Html2pdf.ConverterProperties()
                .SetFontProvider(new iText.Html2pdf.Resolver.Font.DefaultFontProvider(false, true, false)
            );
            iText.Html2pdf.HtmlConverter.ConvertToPdf(html, pdfDocument, settings);
            writer.Close();

            if (string.IsNullOrEmpty(configuration["CertChain"]))
            {
                return pdfStreamEncrypted.ToArray(); // return not signed password protected pdf
            }

            try
            {
                //var pages = pdfDocument.GetNumberOfPages();
                var pk12 = new Org.BouncyCastle.Pkcs.Pkcs12Store(new FileStream(configuration["CertChain"], FileMode.Open, FileAccess.Read), configuration["CertChainPass"].ToCharArray());
                string alias = null;
                foreach (var a in pk12.Aliases)
                {
                    alias = ((string)a);
                    if (pk12.IsKeyEntry(alias))
                    {
                        break;
                    }
                }

                var pk = pk12.GetKey(alias).Key;
                var ce = pk12.GetCertificateChain(alias);
                var chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
                for (var k = 0; k < ce.Length; ++k)
                {
                    chain[k] = ce[k].Certificate;
                }

                return Sign(
                    pdfStreamEncrypted.ToArray(),
                    Encoding.ASCII.GetBytes(configuration["MasterPDFPassword"] ?? ""),
                    chain,
                    pk,
                    iText.Signatures.DigestAlgorithms.SHA512,
                    iText.Signatures.PdfSigner.CryptoStandard.CADES,
                    "Covid test",
                    configuration["SignaturePlace"],
                    //pages
                    1
                    );
            }
            catch (Exception exc)
            {
                logger.LogError(exc, $"Error while signing the pdf document - {exc.Message}");
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
        /// <param name="pages"></param>
        /// <returns></returns>
        public byte[] Sign(
            byte[] src,
            byte[] pass,
            Org.BouncyCastle.X509.X509Certificate[] chain,
            Org.BouncyCastle.Crypto.ICipherParameters pk,
            string digestAlgorithm,
            iText.Signatures.PdfSigner.CryptoStandard subfilter,
            string reason,
            string location,
            int pages
        )
        {
            using var outputMemoryStream = new MemoryStream();
            using var memoryStream = new MemoryStream(src);
            using var signerMemoryStream = new MemoryStream();

            var readerProperties = new iText.Kernel.Pdf.ReaderProperties();
            readerProperties.SetPassword(pass);

            using var pdfReader = new iText.Kernel.Pdf.PdfReader(memoryStream, readerProperties);
            var signer =
                new iText.Signatures.PdfSigner(
                    pdfReader,
                    signerMemoryStream,
                    new iText.Kernel.Pdf.StampingProperties());

            // Create the signature appearance
            var rect = new iText.Kernel.Geom.Rectangle(350, 100, 200, 100);
            var appearance = signer.GetSignatureAppearance();
            appearance.SetReason(reason)
                .SetLocation(location)
                // Specify if the appearance before field is signed will be used
                // as a background for the signed field. The "false" value is the default value.
                .SetReuseAppearance(false)
                .SetPageRect(rect)
                .SetPageNumber(pages);
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
        public virtual async Task<VerificationData> GetResultVerification(string id)
        {
            logger.LogInformation($"VerificationData loaded from database: {id.GetHashCode()}");
            var encoded = await redisCacheClient.Db0.HashGetAsync<string>($"{configuration["db-prefix"]}{REDIS_KEY_RESULTVERIFICATION_OBJECTS}", id.ToString());
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
            var ret = await redisCacheClient.Db0.HashSetAsync($"{configuration["db-prefix"]}{REDIS_KEY_RESULTVERIFICATION_OBJECTS}", verificationData.Id, encoded, mustBeNew);
            if (mustBeNew && !ret)
            {
                throw new Exception("Error creating record in the database");
            }

            return verificationData;
        }
        /// <summary>
        /// Fix. Set to visitor the test result and time of the test
        /// 
        /// tries to match visitors by name with the test results list 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Fix01()
        {
            logger.LogInformation($"Fix01");

            var ret = new List<Visitor>();
            var dict = new Dictionary<string, List<Visitor>>();
            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (!visitor.TestingTime.HasValue || visitor.TestingTime == DateTimeOffset.MinValue)
                    {
                        continue;
                    }
                    var name = $"{visitor.FirstName} {visitor.LastName}";
                    if (!dict.ContainsKey(name))
                    {
                        dict[name] = new List<Visitor>();
                    }
                    dict[name].Add(visitor);
                }
            }
            logger.LogInformation("Visitors cache built");

            foreach (var item in dict.Keys.ToArray())
            {
                var visitor = dict[item].First();
                var firstRC = visitor.RC;
                var name = $"{visitor.FirstName} {visitor.LastName}";
                foreach (var v in dict[item])
                {
                    if (v.RC != firstRC)
                    {
                        logger.LogError($"Multiple people {name}");
                        dict.Remove(item);
                    }
                }
            }

            foreach (var id in await ListAllResultKeys())
            {
                var result = await GetResultVerification(id);
                if (dict.ContainsKey(result.Name))
                {
                    var t = dict[result.Name].FirstOrDefault(x => x.TestingTime.HasValue && x.TestingTime > DateTimeOffset.MinValue)?.TestingTime;
                    if (t.HasValue && result.Time != t)
                    {
                        result.Time = t.Value;
                        await SetResult(result, false);
                        logger.LogInformation("Result fixed");
                    }
                    else
                    {
                        logger.LogInformation($"Item ok");
                    }

                    logger.LogInformation($"Count visitors: {dict[result.Name].Count}");
                    foreach (var visitor in dict[result.Name])
                    {
                        if (string.IsNullOrEmpty(visitor.VerificationId))
                        {
                            visitor.VerificationId = result.Id;
                            await SetVisitor(visitor, false);
                            logger.LogInformation("Visitor fixed");
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"Does not contain name {result.Name}");
                }


            }

            logger.LogInformation($"Fix01 Done");

            return true;
        }
        /// <summary>
        /// Fix. Set to visitor the test result and time of the test
        /// 
        /// tries to match visitors by name with the test results list 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Fix02()
        {
            logger.LogInformation($"Fix02");

            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt);
                    if (visitor == null)
                    {
                        continue;
                    }

                    var name = $"{visitor.FirstName} {visitor.LastName}";

                    if (!string.IsNullOrEmpty(visitor.Phone))
                    {
                        if (visitor.Language == "en")
                        {
                            await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message($"{name}, we are sorry, but your registration {visitorId} was performed in demo application. Please consider it as canceled. Your personal data removed."));
                        }
                        else
                        {
                            await smsSender.SendSMS(visitor.Phone, new Model.SMS.Message($"{name}, ospravedlnujeme sa, vasa registracia {visitorId} bola vykonana do demo aplikacie. Povazujte ju za zrusenu. osobne udaje boli vymazane."));
                        }
                    }
                }
                //await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", visitorId);
            }
            logger.LogInformation($"Fix02 Done");

            return true;
        }
        /// <summary>
        /// Fix. Set to visitor the test result and time of the test
        /// 
        /// tries to match visitors by name with the test results list 
        /// </summary>
        /// <returns></returns>
        public async Task<int> FixBirthYear()
        {
            var ret = 0;
            logger.LogInformation($"FixBirthYear");

            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt, false);
                    if (visitor == null)
                    {
                        continue;
                    }

                    var year = visitor.BirthDayYear;
                    var newVisitor = await FixVisitor(visitor, true);
                    if (
                        year != newVisitor.BirthDayYear &&
                        (
                        visitor.Result == TestResult.PositiveWaitingForCertificate ||
                        visitor.Result == TestResult.NegativeWaitingForCertificate
                        )
                        )
                    {
                        var state = visitor.Result;
                        visitor.Result = TestResult.TestMustBeRepeated;
                        await SetVisitor(visitor, false);// save visitor state
                        await UpdateTestingState(visitor.Id, state, "", false, "", "");
                        ret++;
                    }
                }
                //await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", visitorId);
            }
            logger.LogInformation($"FixBirthYear Done");

            return ret;
        }
        /// <summary>
        /// Fix. Set to visitor the test result and time of the test
        /// 
        /// tries to match visitors by name with the test results list 
        /// </summary>
        /// <returns></returns>
        public async Task<int> FixStats()
        {
            var ret = 0;
            logger.LogInformation($"FixStats");
            var stats = new Dictionary<string, Stat>();

            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt, false);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (!stats.ContainsKey(visitor.ChosenPlaceId))
                    {
                        stats[visitor.ChosenPlaceId] = new Stat();
                    }
                    stats[visitor.ChosenPlaceId].Reg++;
                    if (visitor.Result == TestResult.PositiveCertificateTaken || visitor.Result == TestResult.PositiveWaitingForCertificate)
                    {
                        stats[visitor.ChosenPlaceId].Sick++;
                    }
                    if (visitor.Result == TestResult.NegativeCertificateTaken || visitor.Result == TestResult.NegativeWaitingForCertificate)
                    {
                        stats[visitor.ChosenPlaceId].Healthy++;
                    }
                }
            }

            var places = await placeRepository.ListAll();
            foreach (var p in places)
            {
                if (!stats.ContainsKey(p.Id))
                {
                    continue;
                }

                var fix = false;
                if (p.Registrations != stats[p.Id].Reg)
                {
                    logger.LogInformation($"Fixing stats for {p.Name} Regs: {p.Registrations}");
                    p.Registrations = stats[p.Id].Reg;
                    fix = true;
                    ret++;
                }

                if (p.Sick != stats[p.Id].Sick)
                {
                    logger.LogInformation($"Fixing stats for {p.Name} Sick: {p.Sick}");
                    p.Sick = stats[p.Id].Sick;
                    fix = true;
                    ret++;
                }

                if (p.Healthy != stats[p.Id].Healthy)
                {
                    logger.LogInformation($"Fixing stats for {p.Name} Healthy: {p.Healthy}");
                    p.Healthy = stats[p.Id].Healthy;
                    fix = true;
                    ret++;
                }
                if (fix)
                {
                    await placeRepository.SetPlace(p);
                }
            }

            logger.LogInformation($"FixStats Done");

            return ret;
        }


        public async Task<int> FixVisitorRC()
        {
            var ret = 0;
            logger.LogInformation($"FixVisitorRC");
            var stats = new Dictionary<string, Stat>();

            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt, false);
                    if (visitor == null)
                    {
                        continue;
                    }

                    try
                    {
                        await MapPersonalNumberToVisitorCode(visitor.RC, visitor.Id);
                        ret++;
                    }
                    catch
                    {
                        logger.LogError("Unable to map");
                    }
                }
            }

            logger.LogInformation($"FixVisitorRC Done");

            return ret;
        }
        public async Task<int> FixTestingTime()
        {
            var ret = 0;
            logger.LogInformation($"FixTestingTime");
            var limit = DateTimeOffset.Parse("2021-01-30T08:00:00+01:00");
            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt, false);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (visitor.TestingTime.HasValue)
                    {
                        if (
                            visitor.TestingTime.Value < limit &&
                            visitor.TestingTime.Value.AddDays(3) > DateTimeOffset.Now)
                        {
                            if (visitor.Result == TestResult.NegativeCertificateTaken ||
                                visitor.Result == TestResult.NegativeWaitingForCertificate ||
                                visitor.Result == TestResult.PositiveWaitingForCertificate ||
                                visitor.Result == TestResult.PositiveCertificateTaken)
                            {

                                logger.LogInformation("FixTestingTime.Fixing with result resend");
                                var result = visitor.Result;
                                visitor.TestingTime = DateTimeOffset.Parse("2021-01-30T08:30:00+01:00");
                                visitor.Result = TestResult.TestMustBeRepeated;
                                visitor.ResultNotifiedAt = null;
                                await SetVisitor(visitor, false);
                                await SetTestResult(visitor.TestingSet, result, true);
                            }
                            else
                            {
                                logger.LogInformation("FixTestingTime.Fixing without resend");
                                visitor.TestingTime = DateTimeOffset.Parse("2021-01-30T08:31:00+01:00");
                                await SetVisitor(visitor, false);
                            }
                            ret++;

                        }
                    }
                }
            }

            logger.LogInformation($"FixTestingTime Done");

            return ret;
        }
        /// <summary>
        /// Fix verification data
        /// </summary>
        /// <returns></returns>
        public async Task<int> FixVerificationData()
        {
            var ret = 0;
            logger.LogInformation($"FixTestingTime");
            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    var visitor = await GetVisitor(visitorIdInt, false);
                    if (visitor == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(visitor.VerificationId))
                    {
                        continue;
                    }

                    var updated = false;
                    var data = await GetResultVerification(visitor.VerificationId);
                    if (data == null)
                    {
                        continue;
                    }

                    if (visitor.TestingTime.HasValue && data.Time != visitor.TestingTime)
                    {
                        data.Time = visitor.TestingTime.Value;
                        updated = true;
                    }
                    switch (visitor.Result)
                    {
                        case TestResult.PositiveWaitingForCertificate:
                        case TestResult.PositiveCertificateTaken:

                            if (data.Result != TestResult.PositiveWaitingForCertificate)
                            {
                                data.Result = TestResult.PositiveWaitingForCertificate;
                                updated = true;
                            }
                            break;
                        case TestResult.NegativeWaitingForCertificate:
                        case TestResult.NegativeCertificateTaken:

                            if (data.Result != TestResult.NegativeWaitingForCertificate)
                            {
                                data.Result = TestResult.NegativeWaitingForCertificate;
                                updated = true;
                            }
                            break;
                    }
                    if (updated)
                    {
                        await SetResult(data, false);
                        ret++;
                    }
                }
            }

            logger.LogInformation($"FixTestingTime Done {ret}");

            return ret;
        }
        /// <summary>
        /// FixMapVisitorToDay
        /// </summary>
        /// <returns></returns>
        public async Task<int> FixMapVisitorToDay()
        {
            var ret = 0;
            logger.LogInformation($"FixMapVisitorToDay");
            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    try
                    {
                        var visitor = await GetVisitor(visitorIdInt, false);
                        if (visitor == null)
                        {
                            continue;
                        }

                        if (visitor.TestingTime.HasValue)
                        {
                            var time = new DateTimeOffset(visitor.TestingTime.Value.Year, visitor.TestingTime.Value.Month, visitor.TestingTime.Value.Day, 0, 0, 0, TimeSpan.Zero);
                            await MapDayToVisitorCode(time.Ticks, visitor.Id);
                        }
                        else
                        {
                            var time = new DateTimeOffset(visitor.ChosenSlotTime.Year, visitor.ChosenSlotTime.Month, visitor.ChosenSlotTime.Day, 0, 0, 0, TimeSpan.Zero);
                            await MapDayToVisitorCode(time.Ticks, visitor.Id);
                        }
                        ret++;
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "FixMapVisitorToDay error: " + exc.Message);
                    }
                }
            }

            logger.LogInformation($"FixMapVisitorToDay Done {ret}");

            return ret;
        }
        /// <summary>
        /// Fix person place by day and user
        /// </summary>
        /// <param name="day"></param>
        /// <param name="newPlaceId"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<int> FixPersonPlace(string day, string newPlaceId, string user)
        {

            var ret = 0;
            logger.LogInformation($"FixPersonPlace {day} {newPlaceId} {user}");
            foreach (var visitorId in (await ListAllKeys(DateTimeOffset.Parse(day))))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    try
                    {
                        var visitor = await GetVisitor(visitorIdInt, false);
                        if (visitor == null)
                        {
                            continue;
                        }

                        if (visitor.VerifiedBy == user)
                        {
                            if (visitor.ChosenPlaceId != newPlaceId)
                            {
                                visitor.ChosenPlaceId = newPlaceId;
                                await SetVisitor(visitor, false);
                                ret++;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "FixPersonPlace error: " + exc.Message);
                    }
                }
            }

            logger.LogInformation($"FixPersonPlace Done {ret}");

            return ret;
        }

        public async Task<int> FixSendRegistrationSMS()
        {
            var ret = 0;
            logger.LogInformation($"FixSendRegistrationSMS");
            foreach (var visitorId in (await ListAllKeys()))
            {
                if (int.TryParse(visitorId, out var visitorIdInt))
                {
                    try
                    {
                        var visitor = await GetVisitor(visitorIdInt, false);
                        if (visitor == null)
                        {
                            continue;
                        }

                        if (visitor.ChosenPlaceId != "BA333")
                        {
                            continue;
                        }

                        if (visitor.ChosenSlot < 637481916000000000)
                        {
                            continue;
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

                            var pdf = GenerateRegistrationPDF(visitor, pp?.CompanyName, place?.Name, place?.Address, product?.Name);
                            attachments.Add(new SendGrid.Helpers.Mail.Attachment()
                            {
                                Content = Convert.ToBase64String(pdf),
                                Filename = $"reg-{visitor.LastName}{visitor.FirstName}-{slot.TimeInCET.ToString("MMdd")}.pdf",
                                Type = "application/pdf",
                                Disposition = "attachment"
                            });
                        }
                        catch (Exception exc)
                        {
                            logger.LogError(exc, "Error generating file");
                        }
                        var code = visitor.Id.ToString();

                        await emailSender.SendEmail(
                            localizer[Repository_RedisRepository_VisitorRepository.Covid_test],
                            visitor.Email,
                            $"{visitor.FirstName} {visitor.LastName}",
                            new Model.Email.VisitorRegistrationEmail(visitor.Language, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                            {
                                Code = $"{code.Substring(0, 3)}-{code.Substring(3, 3)}-{code.Substring(6, 3)}",
                                Name = $"{visitor.FirstName} {visitor.LastName}",
                                Date = $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}",
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
                                    $"{slot.TimeInCET.ToString("dd.MM.yyyy")} {slot.Description}",
                                    place.Name
                            )));
                        }
                        CultureInfo.CurrentCulture = oldCulture;
                        CultureInfo.CurrentUICulture = oldUICulture;
                        ret++;
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "FixSendRegistrationSMS error: " + exc.Message);
                    }
                }
            }

            logger.LogInformation($"FixSendRegistrationSMS Done {ret}");

            return ret;
        }

        /// <summary>
        /// Fix. Set to visitor the test result and time of the test
        /// 
        /// tries to match visitors by name with the test results list 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Fix03()
        {
            logger.LogInformation($"Fix03");

            foreach (var visitorId in (await ListAllKeys()))
            {
                await redisCacheClient.Db0.HashDeleteAsync($"{configuration["db-prefix"]}{REDIS_KEY_VISITORS_OBJECTS}", visitorId);
            }
            logger.LogInformation($"Fix03 Done");

            return true;
        }
    }
}
