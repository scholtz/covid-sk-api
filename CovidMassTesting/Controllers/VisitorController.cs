using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// This controller manages public registrations
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class VisitorController : ControllerBase
    {
        private readonly ILogger<VisitorController> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly IUserRepository userRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly GoogleReCaptcha.V3.Interface.ICaptchaValidator captchaValidator;
        private readonly IConfiguration configuration;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="configuration"></param>
        /// <param name="captchaValidator"></param>
        public VisitorController(
            ILogger<VisitorController> logger,
            IVisitorRepository visitorRepository,
            IUserRepository userRepository,
            IPlaceProviderRepository placeProviderRepository,
            IConfiguration configuration,
            GoogleReCaptcha.V3.Interface.ICaptchaValidator captchaValidator
            )
        {
            this.logger = logger;
            this.visitorRepository = visitorRepository;
            this.configuration = configuration;
            this.captchaValidator = captchaValidator;
            this.userRepository = userRepository;
            this.placeProviderRepository = placeProviderRepository;
        }
        /// <summary>
        /// Public method for pre registration. Result is returned with Visitor.id which is the main identifier of the visit and should be shown in the bar code
        /// 
        /// Request size is limitted.
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        [HttpPost("Register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> Register([FromBody] Visitor visitor)
        {
            try
            {
                if (visitor is null)
                {
                    throw new ArgumentNullException(nameof(visitor));
                }
                if (!string.IsNullOrEmpty(configuration["googleReCaptcha:SiteKey"]))
                {
                    if (string.IsNullOrEmpty(visitor.Token))
                    {
                        throw new Exception("Please provide captcha");
                    }

                    var validation = await captchaValidator.IsCaptchaPassedAsync(visitor.Token);
                    if (!validation)
                    {
                        throw new Exception("Please provide valid captcha");
                    }
                    visitor.Token = "";
                }
                var time = new DateTimeOffset(visitor.ChosenSlot, TimeSpan.Zero);
                if (time.AddMinutes(10) < DateTimeOffset.Now)
                {
                    throw new Exception("Na tento termín sa nedá zaregistrovať pretože časový úsek je už ukončený");
                }

                visitor.Address = $"{visitor.Street} {visitor.StreetNo}, {visitor.ZIP} {visitor.City}";

                return Ok(await visitorRepository.Register(visitor, ""));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Registration manager can register visitor
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterByManager")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> RegisterByManager([FromBody] Visitor visitor)
        {
            try
            {
                if (visitor is null)
                {
                    throw new ArgumentNullException(nameof(visitor));
                }
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository)
                    && !User.IsMedicTester(userRepository, placeProviderRepository))
                    throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to register user at the place");

                return Ok(await visitorRepository.Register(visitor, User.GetEmail()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
