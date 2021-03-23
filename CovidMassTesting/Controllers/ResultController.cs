using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using CsvHelper;
using GoogleReCaptcha.V3.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// This controller manages test results
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ResultController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<ResultController> localizer;
        private readonly ILogger<ResultController> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly IUserRepository userRepository;
        private readonly IPlaceRepository placeRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly ICaptchaValidator captchaValidator;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="localizer"></param>
        /// <param name="logger"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="captchaValidator"></param>
        public ResultController(
            IConfiguration configuration,
            IStringLocalizer<ResultController> localizer,
            ILogger<ResultController> logger,
            IVisitorRepository visitorRepository,
            IUserRepository userRepository,
            IPlaceRepository placeRepository,
            IPlaceProviderRepository placeProviderRepository,
            ICaptchaValidator captchaValidator
            )
        {
            this.configuration = configuration;
            this.localizer = localizer;
            this.logger = logger;
            this.visitorRepository = visitorRepository;
            this.userRepository = userRepository;
            this.placeRepository = placeRepository;
            this.placeProviderRepository = placeProviderRepository;
            this.captchaValidator = captchaValidator;
        }
        /// <summary>
        /// Testing personell can load data by the code, so that they can verify that the code is the specific user
        /// </summary>
        /// <param name="visitorCode"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetVisitor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Visitor>> GetVisitor([FromForm] string visitorCode)
        {
            try
            {
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository) && !User.IsMedicTester(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Registration_Manager_role_or_Medic_Tester_role_is_allowed_to_fetch_data_of_visitors].Value);
                }

                if (string.IsNullOrEmpty(visitorCode))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Visitor_code_must_not_be_empty].Value);
                }

                var codeClear = FormatBarCode(visitorCode);
                var testCodeClear = FormatBarCode(visitorCode);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.GetVisitor(codeInt));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code].Value);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Testing personell can load data by the personal number, so that they can verify that person legitimity
        /// </summary>
        /// <param name="rc">Personal number</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetVisitorByRC")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Visitor>> GetVisitorByRC([FromForm] string rc)
        {
            try
            {
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository) && !User.IsMedicTester(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer["Only user with Registration Manager role or Medic Tester role is allowed to fetch data of visitors"].Value);
                }

                if (string.IsNullOrEmpty(rc))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Personal_number_must_not_be_empty].Value);
                }
                return Ok(await visitorRepository.GetVisitorByPersonalNumber(rc));
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// This method is for triage person who scans the visitor bar code, scans the testing set bar code and performs test.
        /// </summary>
        /// <param name="visitorCode"></param>
        /// <param name="testCode"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ConnectVisitorToTest")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> ConnectVisitorToTest([FromForm] string visitorCode, [FromForm] string testCode)
        {
            try
            {
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository) && !User.IsMedicTester(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer["Only user with Registration Manager role or Medic Tester role is allowed to register user to test"].Value);
                }

                if (string.IsNullOrEmpty(visitorCode))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Visitor_code_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(testCode))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Test_code_must_not_be_empty].Value);
                }


                var codeClear = FormatBarCode(visitorCode);
                var testCodeClear = FormatBarCode(testCode);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.ConnectVisitorToTest(codeInt, testCodeClear, User.GetEmail(), User.GetPlaceProvider(), HttpContext.GetIPAddress()));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code]);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Public method to show test results to user
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        [HttpPost("Get")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> Get([FromForm] string code, [FromForm] string pass, [FromForm] string captcha = "")
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Visitor_code_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Last_4_digits_of_personal_number_or_declared_passport_for_foreigner_at_registration_must_not_be_empty].Value);
                }

                if (!string.IsNullOrEmpty(configuration["googleReCaptcha:SiteKey"]))
                {
                    if (string.IsNullOrEmpty(captcha))
                    {
                        throw new Exception("Please provide captcha");
                    }

                    var validation = await captchaValidator.IsCaptchaPassedAsync(captcha);
                    if (!validation)
                    {
                        throw new Exception("Please provide valid captcha");
                    }
                }

                var codeClear = FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.GetTest(codeInt, pass));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code]);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Public method to show test results to user .. returns pdf file
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        [HttpPost("DownloadPDF")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> DownloadPDF([FromForm] string code, [FromForm] string pass, [FromForm] string captcha)
        {

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Visitor_code_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Last_4_digits_of_personal_number_or_declared_passport_for_foreigner_at_registration_must_not_be_empty].Value);
                }
                if (!string.IsNullOrEmpty(configuration["googleReCaptcha:SiteKey"]))
                {
                    if (string.IsNullOrEmpty(captcha))
                    {
                        throw new Exception("Please provide captcha");
                    }

                    var validation = await captchaValidator.IsCaptchaPassedAsync(captcha);
                    if (!validation)
                    {
                        throw new Exception("Please provide valid captcha");
                    }
                }


                var codeClear = FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    var data = await visitorRepository.GetPublicPDF(codeInt, pass);

                    return File(data, "application/pdf", "result.pdf");
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code]);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// PrintCertificateByDocumentManager
        /// </summary>
        /// <param name="registrationCode"></param>
        /// <param name="personalNumber"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("PrintCertificateByDocumentManager")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> PrintCertificateByDocumentManager([FromForm] string registrationCode, [FromForm] string personalNumber)
        {
            try
            {
                if (!User.IsDocumentManager(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Document_Manager_role_is_allowed_to_move_the_queue_forward].Value);
                }

                var normalizePersonalNumber = visitorRepository.FormatDocument(personalNumber);
                if (!string.IsNullOrEmpty(normalizePersonalNumber))
                {
                    var visitor = await visitorRepository.GetVisitorByPersonalNumber(personalNumber);
                    if (visitor != null)
                    {

                        var data = await visitorRepository.GetResultPDFByEmployee(visitor.Id, User.GetEmail());
                        return File(data, "application/pdf", "result.pdf");
                    }
                }
                var codeClear = FormatBarCode(registrationCode);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    var data = await visitorRepository.GetResultPDFByEmployee(codeInt, User.GetEmail());
                    return File(data, "application/pdf", "result.pdf");
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code]);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Person can resend his results one more time on request
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        [HttpPost("ResendResult")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> ResendResult([FromForm] string code, [FromForm] string pass, [FromForm] string captcha)
        {

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Visitor_code_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Last_4_digits_of_personal_number_or_declared_passport_for_foreigner_at_registration_must_not_be_empty].Value);
                }
                if (!string.IsNullOrEmpty(configuration["googleReCaptcha:SiteKey"]))
                {
                    if (string.IsNullOrEmpty(captcha))
                    {
                        throw new Exception("Please provide captcha");
                    }

                    var validation = await captchaValidator.IsCaptchaPassedAsync(captcha);
                    if (!validation)
                    {
                        throw new Exception("Please provide valid captcha");
                    }
                }

                var codeClear = FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.ResendResults(codeInt, pass));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code]);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Public method to remove user test from database and all his private information
        /// 
        /// It is possible to remove this test only when test is marked as negative
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        [HttpPost("RemoveTest")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> RemoveTest([FromForm] string code, [FromForm] string pass, [FromForm] string captcha = "")
        {

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Visitor_code_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Last_4_digits_of_personal_number_or_declared_passport_for_foreigner_at_registration_must_not_be_empty].Value);
                }
                if (!string.IsNullOrEmpty(configuration["googleReCaptcha:SiteKey"]))
                {
                    if (string.IsNullOrEmpty(captcha))
                    {
                        throw new Exception("Please provide captcha");
                    }

                    var validation = await captchaValidator.IsCaptchaPassedAsync(captcha);
                    if (!validation)
                    {
                        throw new Exception("Please provide valid captcha");
                    }
                }
                var codeClear = FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.RemoveTest(codeInt, pass));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code].Value);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// This method is for triage person who scans the visitor bar code, scans the testing set bar code and performs test.
        /// </summary>
        /// <param name="testCode"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetResult")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> SetResult([FromForm] string testCode, [FromForm] string result)
        {
            try
            {
                if (!User.IsMedicLab(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Medic_Lab_role_is_allowed_to_set_results_of_tests].Value);
                }

                if (string.IsNullOrEmpty(testCode))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Code_of_the_test_set_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(result))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Result_of_the_test_must_not_be_empty].Value);
                }
                var isAdmin = await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository);
                switch (result)
                {
                    case TestResult.NegativeWaitingForCertificate:
                    case TestResult.PositiveWaitingForCertificate:
                    case TestResult.TestMustBeRepeated:
                        return Ok(await visitorRepository.SetTestResult(FormatBarCode(testCode), result, isAdmin));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_result_state].Value);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// This method is for person who writes certificates on paper
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetNextTest")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Visitor>> GetNextTest()
        {
            try
            {
                if (!User.IsDocumentManager(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Document_Manager_role_is_allowed_to_fetch_visitor_data].Value);
                }

                return Ok(await visitorRepository.GetNextTest());
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// This method removes test from queue
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveFromDocQueue")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> RemoveFromDocQueue([FromForm] string testId)
        {
            try
            {
                if (string.IsNullOrEmpty(testId))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Test_id_must_not_be_empty].Value);
                }

                if (!User.IsDocumentManager(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Document_Manager_role_is_allowed_to_move_the_queue_forward].Value);
                }

                var isAdmin = await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository);
                var code = FormatBarCode(testId);
                var ret = await visitorRepository.RemoveFromDocQueueAndSetTestStateAsTaken(code, isAdmin);
                return Ok(ret);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }


        /// <summary>
        /// This method removes test from queue
        /// </summary>
        /// <returns></returns>
        [HttpPost("VerifyResult")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<VerificationData>> VerifyResult([FromForm] string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Please provide verification identifier");
                }

                var ret = await visitorRepository.GetResultVerification(id);
                return Ok(ret);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// This method exports data for healthy office
        /// 
        /// Lists all visitor which were marked as sick. Exports all available data.
        /// 
        /// returns CSV file as download
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("FinalDataExport")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> FinalDataExport([FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"User {User.GetEmail()} is exporting sick visitors {day}");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var data = await visitorRepository.ListSickVisitors(User.GetPlaceProvider(), day, from, count);
                var places = (await placeRepository.ListAll()).Where(place => place.PlaceProviderId == User.GetPlaceProvider()).Select(p => p.Id).ToHashSet();
                data = data.Where(p => places.Contains(p.ChosenPlaceId));

                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"final-data-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// List visitors tested at specified day
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListTestedVisitors")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ListTestedVisitors([FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"User {User.GetEmail()} is exporting tested visitors {day}");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ListTestedVisitors(User.GetPlaceProvider(), day, from, count);
                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                var name = $"all-tested-alldays-{from}-{count}.csv";
                if (day.HasValue)
                {
                    name = $"all-tested-{day.Value.Ticks}-{from}-{count}.csv";
                }
                return File(ret, "text/csv", name);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// List visitors tested at specified day
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListAnonymizedVisitors")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ListAnonymizedVisitors([FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                var isAdmin = false;
                if (User.IsAdmin(userRepository))
                {
                    // ok
                    isAdmin = true;
                }
                else
                {
                    if (!User.IsDataExporter(userRepository, placeProviderRepository))
                    {
                        throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                    }
                }
                logger.LogInformation($"ListAnonymizedVisitors: User {User.GetEmail()} is exporting anonymized visitors {day}");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

                var data = await visitorRepository.ListAnonymizedVisitors(day, from, count);

                if (!isAdmin)
                {
                    var places = (await placeRepository.ListAll()).Where(place => place.PlaceProviderId == User.GetPlaceProvider()).Select(p => p.Id).ToHashSet();
                    data = data.Where(p => places.Contains(p.ChosenPlaceId));
                }

                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"ListAnonymizedVisitors: Export size: {ret.Length}");
                var name = $"all-anonymized-alldays-{from}-{count}.csv";
                if (day.HasValue)
                {
                    name = $"all-anonymized-{day.Value.Ticks}-{from}-{count}.csv";
                }
                return File(ret, "text/csv", name);
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        [Authorize]
        [HttpGet("ExportResultSubmissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ExportResultSubmissions([FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (
                    !User.IsDataExporter(userRepository, placeProviderRepository)
                        &&
                    !await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)
                )
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"ExportResultSubmissions: {User.GetEmail()} is exporting");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var places = (await placeRepository.ListAll()).Where(place => place.PlaceProviderId == User.GetPlaceProvider()).Select(p => p.Id).ToHashSet();
                var data = await visitorRepository.ExportResultSubmissions(from, count, places);

                if (day.HasValue)
                {
                    data = data.Where(d => d.Time >= day.Value.Date && d.Time < day.Value.Date.AddDays(1));
                }

                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"final-data-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Proof of work.. Export for army or other institution with names of visitors
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ProofOfWorkExport")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ProofOfWorkExport([FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"User {User.GetEmail()} is exporting sick visitors");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ProofOfWorkExport(day, from, count, User.GetPlaceProvider());
                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"final-data-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        [Authorize]
        [HttpGet("CustomExport")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> CustomExport(string exportType, [FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"CustomExport: User {User.GetEmail()} is exporting CustomExport {exportType}");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ListTestedVisitors(User.GetPlaceProvider(), day, from, count);
                logger.LogInformation($"Found: {data.Count()} records");
                switch (exportType)
                {
                    case "aures-1":
                        var customdata = data.Select(x =>
                            new
                            {
                                Prijmeni = x.LastName,
                                Jmeno = x.FirstName,
                                DatumNarozeni = $"{x.BirthDayDay}.{x.BirthDayMonth}.{x.BirthDayYear}",
                                CisloPijistence = x.RC,
                                StatPrislusnost = x.Nationality,
                                ZdravotniPojistovna = x.Insurance,
                                Mesto = x.City,
                                PSC = x.ZIP,
                                Telefon = x.Phone,
                                Vysledek = x.Result,
                                Poznaka = $"{x.EmployeeId}|{x.PlaceName}|{x.ProductName}|rychlejsie.sk"
                            });
                        csv.WriteRecords(customdata);
                        break;
                    default:
                        csv.WriteRecords(data);
                        break;
                }

                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"CustomExport: done {ret.Length}");
                return File(ret, "text/csv", $"final-data-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// ListExportableDays
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListExportableDays")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<TextValue>>> ListExportableDays()
        {
            try
            {
                if (
                    !User.IsDataExporter(userRepository, placeProviderRepository)
                        &&
                    !await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)
                )
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"ListExportableDays: {User.GetEmail()}");

                return Ok((await visitorRepository.ListExportableDays()).Select(t => new TextValue()
                {
                    Text = t.ToString("dd.MM.yyyy"),
                    Value = t.ToString("o")
                }));
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// This method exports all visitors who are in state in processing
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListVisitorsInProcess")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ListVisitorsInProcess([FromQuery] DateTimeOffset? day = null, [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"User {User.GetEmail()} is exporting visitors in process");
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ListVisitorsInProcess(User.GetPlaceProvider(), day, from, count);
                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"not-processed-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }


        /// <summary>
        /// This method exports all visitors who did not come for the test
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListAllVisitorsWhoDidNotCome")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ListAllVisitorsWhoDidNotCome(
            [FromQuery] DateTimeOffset? day = null,
            [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"User {User.GetEmail()} is exporting visitors in process");
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ListAllVisitorsWhoDidNotCome(User.GetPlaceProvider(), day, from, count);
                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"not-visited-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Export all visitors
        /// </summary>
        /// <param name="day"></param>
        /// <param name="from"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListAllVisitors")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ListAllVisitors(
            [FromQuery] DateTimeOffset? day = null,
            [FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository))
                {
                    throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                }

                logger.LogInformation($"User {User.GetEmail()} is exporting visitors in process");
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ListAllVisitors(User.GetPlaceProvider(), day, from, count);
                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"all-visitors-export-{from}-{count}.csv");
            }
            catch (ArgumentException exc)
            {
                logger.LogError(exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }


        /// <summary>
        /// Format the visitor code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string FormatBarCode(string code)
        {
            return code.FormatBarCode();
        }
    }
}
