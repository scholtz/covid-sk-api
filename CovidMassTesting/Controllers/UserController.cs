using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CovidMassTesting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<PlaceController> logger;
        private readonly IUserRepository userRepository;
        public UserController(
            ILogger<PlaceController> logger,
            IUserRepository userRepository
            )
        {
            this.logger = logger;
            this.userRepository = userRepository;
        }

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

    }
}
