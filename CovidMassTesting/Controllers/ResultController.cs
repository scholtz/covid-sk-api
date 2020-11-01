using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<ResultController> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly IUserRepository userRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="userRepository"></param>
        public ResultController(
            ILogger<ResultController> logger,
            IVisitorRepository visitorRepository,
            IUserRepository userRepository
            )
        {
            this.logger = logger;
            this.visitorRepository = visitorRepository;
            this.userRepository = userRepository;
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
                if (!User.IsRegistrationManager(userRepository) && !User.IsMedicTester(userRepository)) throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to fetch data of visitors");

                if (string.IsNullOrEmpty(visitorCode))
                {
                    throw new ArgumentException($"'{nameof(visitorCode)}' cannot be null or empty", nameof(visitorCode));
                }

                var codeClear = FormatBarCode(visitorCode);
                var testCodeClear = FormatBarCode(visitorCode);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.GetVisitor(codeInt));
                }
                throw new Exception("Invalid visitor code");
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
                if (!User.IsRegistrationManager(userRepository) && !User.IsMedicTester(userRepository)) throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to fetch data of visitors");

                if (string.IsNullOrEmpty(rc))
                {
                    throw new ArgumentException($"'{nameof(rc)}' cannot be null or empty", nameof(rc));
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
                if (!User.IsRegistrationManager(userRepository) && !User.IsMedicTester(userRepository)) throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to register user to test");


                if (string.IsNullOrEmpty(visitorCode))
                {
                    throw new ArgumentException($"'{nameof(visitorCode)}' cannot be null or empty", nameof(visitorCode));
                }

                if (string.IsNullOrEmpty(testCode))
                {
                    throw new ArgumentException($"'{nameof(testCode)}' cannot be null or empty", nameof(testCode));
                }


                var codeClear = FormatBarCode(visitorCode);
                var testCodeClear = FormatBarCode(testCode);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.ConnectVisitorToTest(codeInt, testCodeClear));
                }
                throw new Exception("Invalid visitor code");
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
        /// <returns></returns>
        [HttpPost("Get")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> Get([FromForm] string code, [FromForm] string pass)
        {

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException($"'{nameof(code)}' cannot be null or empty", nameof(code));
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException($"'{nameof(pass)}' cannot be null or empty", nameof(pass));
                }
                var codeClear = FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.GetTest(codeInt, pass));
                }
                throw new Exception("Invalid code");
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
        /// <returns></returns>
        [HttpPost("RemoveTest")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> RemoveTest([FromForm] string code, [FromForm] string pass)
        {

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException($"'{nameof(code)}' cannot be null or empty", nameof(code));
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException($"'{nameof(pass)}' cannot be null or empty", nameof(pass));
                }
                var codeClear = FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.RemoveTest(codeInt, pass));
                }
                throw new Exception("Invalid code");
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
                if (!User.IsMedicLab(userRepository)) throw new Exception("Only user with Medic Lab role is allowed to set results of tests");

                if (string.IsNullOrEmpty(testCode))
                {
                    throw new ArgumentException($"'{nameof(testCode)}' cannot be null or empty", nameof(testCode));
                }

                if (string.IsNullOrEmpty(result))
                {
                    throw new ArgumentException($"'{nameof(result)}' cannot be null or empty", nameof(result));
                }

                switch (result)
                {
                    case TestResult.NegativeWaitingForCertificate:
                    case TestResult.PositiveWaitingForCertificate:
                    case TestResult.TestMustBeRepeated:
                        return Ok(await visitorRepository.SetTestResult(FormatBarCode(testCode), result));
                }
                throw new Exception("Invalid state");
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
                if (!User.IsDocumentManager(userRepository)) throw new Exception("Only user with Document Manager role is allowed to fetch visitor data");

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
                    throw new ArgumentException($"'{nameof(testId)}' cannot be null or empty", nameof(testId));
                }

                if (!User.IsDocumentManager(userRepository)) throw new Exception("Only user with Document Manager role is allowed to fetch visitor data");

                return Ok(await visitorRepository.RemoveFromDocQueue(FormatBarCode(testId)));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        private static string FormatBarCode(string code)
        {
            return code
                .Replace("-", "")
                .Replace(" ", "")
                .Trim();
        }
    }
}
