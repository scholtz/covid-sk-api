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
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
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
                if (string.IsNullOrEmpty(visitor.Address))
                {
                    visitor.Address = $"{visitor.Street} {visitor.StreetNo}, {visitor.ZIP} {visitor.City}";
                }

                if (visitor.PersonType == "foreign")
                {
                    if (string.IsNullOrEmpty(visitor.Passport)) throw new Exception("Zadajte číslo cestovného dokladu prosím");
                }
                else
                {
                    if (string.IsNullOrEmpty(visitor.RC)) throw new Exception("Zadajte rodné číslo prosím");
                }

                if (string.IsNullOrEmpty(visitor.Street)) throw new Exception("Zadajte ulicu trvalého bydliska prosím");
                if (string.IsNullOrEmpty(visitor.StreetNo)) throw new Exception("Zadajte číslo domu trvalého bydliska prosím");
                if (string.IsNullOrEmpty(visitor.ZIP)) throw new Exception("Zadajte PSČ trvalého bydliska prosím");
                if (string.IsNullOrEmpty(visitor.City)) throw new Exception("Zadajte mesto trvalého bydliska prosím");

                if (string.IsNullOrEmpty(visitor.FirstName)) throw new Exception("Zadajte svoje meno prosím");
                if (string.IsNullOrEmpty(visitor.LastName)) throw new Exception("Zadajte svoje priezvisko prosím");

                if (!visitor.BirthDayYear.HasValue || visitor.BirthDayYear < 1900 || visitor.BirthDayYear > 2021) throw new Exception("Rok Vášho narodenia vyzerá byť chybne vyplnený");
                if (!visitor.BirthDayDay.HasValue || visitor.BirthDayDay < 1 || visitor.BirthDayDay > 31) throw new Exception("Deň Vášho narodenia vyzerá byť chybne vyplnený");
                if (!visitor.BirthDayMonth.HasValue || visitor.BirthDayMonth < 1 || visitor.BirthDayMonth > 12) throw new Exception("Mesiac Vášho narodenia vyzerá byť chybne vyplnený");

                return Ok(await visitorRepository.Register(visitor, ""));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// When person comes to the queue he can mark him as in the queue
        /// 
        /// It can help other people to check the queue time
        /// </summary>
        /// <param name="code"></param>
        /// <param name="pass"></param>
        /// <param name="captcha"></param>
        /// <returns></returns>
        [HttpPost("Enqueued")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> Enqueued([FromForm] string code, [FromForm] string pass, [FromForm] string captcha = "")
        {

            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    throw new ArgumentException("Zadajte svoj 9 miestny kód registrácie");
                }

                if (string.IsNullOrEmpty(pass))
                {
                    throw new ArgumentException("Zadajte posledné štvorčíslie svojho rodného čísla");
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
                var codeClear = ResultController.FormatBarCode(code);
                if (int.TryParse(codeClear, out var codeInt))
                {
                    return Ok(await visitorRepository.Enqueued(codeInt, pass));
                }
                throw new Exception("Registračný kód vyzerá byť neplatný");
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
