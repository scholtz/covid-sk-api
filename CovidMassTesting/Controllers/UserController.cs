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
    /// Controller that manages user requests
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<PlaceController> logger;
        private readonly IUserRepository userRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userRepository"></param>
        public UserController(
            ILogger<PlaceController> logger,
            IUserRepository userRepository
            )
        {
            this.logger = logger;
            this.userRepository = userRepository;
        }
        /// <summary>
        /// List all public information of all users
        /// </summary>
        /// <returns></returns>

        [Authorize]
        [HttpGet("List")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, UserPublic>>> List()
        {
            try
            {
                return Ok((await userRepository.ListAll()).ToDictionary(p => p.Email, p => p.ToPublic()));
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message + (exc.InnerException != null ? $";\n{exc.InnerException.Message}" : "") + "\n" + exc.StackTrace, Title = exc.Message, Type = exc.GetType().ToString() });
            }
        }

        /// <summary>
        /// Preauthenticate. Cohash is important part of hash. This method returns cohash
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost("Preauthenticate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> Preauthenticate(
            [FromForm] string email
            )
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException($"'{nameof(email)}' cannot be null or empty", nameof(email));
                }

                return Ok(await userRepository.Preauthenticate(email));
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message + (exc.InnerException != null ? $";\n{exc.InnerException.Message}" : "") + "\n" + exc.StackTrace, Title = exc.Message, Type = exc.GetType().ToString() });
            }
        }
        /// <summary>
        /// Returns JWT token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("Authenticate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> Authenticate(
            [FromForm] string email,
            [FromForm] string hash,
            [FromForm] string data)
        {
            try
            {
                return Ok(await userRepository.Authenticate(email, hash, data));
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message + (exc.InnerException != null ? $";\n{exc.InnerException.Message}" : "") + "\n" + exc.StackTrace, Title = exc.Message, Type = exc.GetType().ToString() });
            }
        }
    }
}
