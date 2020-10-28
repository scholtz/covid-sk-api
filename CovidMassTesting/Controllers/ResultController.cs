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
    [ApiController]
    [Route("[controller]")]
    public class ResultController : ControllerBase
    {
        private readonly ILogger<ResultController> logger;
        private readonly IVisitorRepository visitorRepository;
        public ResultController(
            ILogger<ResultController> logger,
            IVisitorRepository visitorRepository
            )
        {
            this.logger = logger;
            this.visitorRepository = visitorRepository;
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

                if (string.IsNullOrEmpty(visitorCode))
                {
                    throw new ArgumentException($"'{nameof(visitorCode)}' cannot be null or empty", nameof(visitorCode));
                }

                if (string.IsNullOrEmpty(testCode))
                {
                    throw new ArgumentException($"'{nameof(testCode)}' cannot be null or empty", nameof(testCode));
                }


                var codeClear = visitorCode.Replace("-", "").Replace(" ", "").Trim();
                var testCodeClear = visitorCode.Replace("-", "").Replace(" ", "").Trim();
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.ConnectVisitorToTest(codeInt, testCodeClear));
                }
                throw new Exception("Invalid visitor code");
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        [HttpPost("Get")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Result>> List([FromForm] string code, [FromForm] string pass)
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
                var codeClear = code.Replace("-", "").Replace(" ", "").Trim();
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.GetTest(codeInt, pass));
                }
                throw new Exception("Invalid code");
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

    }
}
