using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using CsvHelper;
using GoogleReCaptcha.V3.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

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
        /// <param name="placeProviderRepository"></param>
        /// <param name="captchaValidator"></param>
        public ResultController(
            IConfiguration configuration,
            IStringLocalizer<ResultController> localizer,
            ILogger<ResultController> logger,
            IVisitorRepository visitorRepository,
            IUserRepository userRepository,
            IPlaceProviderRepository placeProviderRepository,
            ICaptchaValidator captchaValidator
            )
        {
            this.configuration = configuration;
            this.localizer = localizer;
            this.logger = logger;
            this.visitorRepository = visitorRepository;
            this.userRepository = userRepository;
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
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository) && !User.IsMedicTester(userRepository, placeProviderRepository)) throw new Exception(localizer["Only user with Registration Manager role or Medic Tester role is allowed to fetch data of visitors"].Value);

                if (string.IsNullOrEmpty(rc))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Personal_number_must_not_be_empty].Value);
                }
                return Ok(await visitorRepository.GetVisitorByPersonalNumber(rc));
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
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository) && !User.IsMedicTester(userRepository, placeProviderRepository)) throw new Exception(localizer["Only user with Registration Manager role or Medic Tester role is allowed to register user to test"].Value);


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
                    return Ok(await visitorRepository.ConnectVisitorToTest(codeInt, testCodeClear));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code]);
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
                    return Ok(await visitorRepository.RemoveTest(codeInt, pass, true));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_visitor_code].Value);
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
                if (!User.IsMedicLab(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_ResultController.Only_user_with_Medic_Lab_role_is_allowed_to_set_results_of_tests].Value);

                if (string.IsNullOrEmpty(testCode))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Code_of_the_test_set_must_not_be_empty].Value);
                }

                if (string.IsNullOrEmpty(result))
                {
                    throw new ArgumentException(localizer[Controllers_ResultController.Result_of_the_test_must_not_be_empty].Value);
                }

                switch (result)
                {
                    case TestResult.NegativeWaitingForCertificate:
                    case TestResult.PositiveWaitingForCertificate:
                    case TestResult.TestMustBeRepeated:
                        return Ok(await visitorRepository.SetTestResult(FormatBarCode(testCode), result));
                }
                throw new Exception(localizer[Controllers_ResultController.Invalid_result_state].Value);
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
                if (!User.IsDocumentManager(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_ResultController.Only_user_with_Document_Manager_role_is_allowed_to_fetch_visitor_data].Value);

                return Ok(await visitorRepository.GetNextTest());
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

                if (!User.IsDocumentManager(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_ResultController.Only_user_with_Document_Manager_role_is_allowed_to_move_the_queue_forward].Value);
                var code = FormatBarCode(testId);
                var ret = await visitorRepository.RemoveFromDocQueueAndSetTestStateAsTaken(code);
                return Ok(ret);
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

                var ret = await visitorRepository.GetResult(id);
                return Ok(ret);
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
        public async Task<ActionResult> FinalDataExport([FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                logger.LogInformation($"User {User.GetEmail()} is exporting sick visitors");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                writer.Write("Test");
                var data = await visitorRepository.ListSickVisitors(from, count);

                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"final-data-export-{from}-{count}.csv");
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
        public async Task<ActionResult> ProofOfWorkExport([FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                logger.LogInformation($"User {User.GetEmail()} is exporting sick visitors");

                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                writer.Write("Test");
                var data = await visitorRepository.ProofOfWorkExport(from, count);

                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"final-data-export-{from}-{count}.csv");
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
        public async Task<ActionResult> ListVisitorsInProcess([FromQuery] int from = 0, [FromQuery] int count = 9999999)
        {
            try
            {
                if (!User.IsDataExporter(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_ResultController.Only_user_with_Data_Exporter_role_is_allowed_to_fetch_all_sick_visitors].Value);
                logger.LogInformation($"User {User.GetEmail()} is exporting visitors in process");
                using var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                var data = await visitorRepository.ListVisitorsInProcess(from, count);
                csv.WriteRecords(data);
                writer.Flush();
                var ret = stream.ToArray();
                logger.LogInformation($"Export size: {ret.Length}");
                return File(ret, "text/csv", $"not-processed-export-{from}-{count}.csv");
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
            return code
                .Replace("-", "")
                .Replace(" ", "")
                .Trim();
        }
    }
}
