using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CovidMassTesting.Helpers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SlugGenerator;

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
        private readonly IPlaceRepository placeRepository;
        private readonly GoogleReCaptcha.V3.Interface.ICaptchaValidator captchaValidator;
        private readonly IConfiguration configuration;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="configuration"></param>
        /// <param name="captchaValidator"></param>
        public VisitorController(
            ILogger<VisitorController> logger,
            IVisitorRepository visitorRepository,
            IUserRepository userRepository,
            IPlaceProviderRepository placeProviderRepository,
            IPlaceRepository placeRepository,
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
            this.placeRepository = placeRepository;
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
                visitor.RegistrationTime = DateTimeOffset.UtcNow;
                visitor.SelfRegistration = true;
                return Ok(await visitorRepository.Register(visitor, ""));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Public method for pre registration of company workers.
        /// </summary>
        /// <returns></returns>
        [HttpPost("RegisterWithCompanyRegistration")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> RegisterWithCompanyRegistration(
            [FromForm] long chosenSlotId,
            [FromForm] string chosenPlaceId,
            [FromForm] string personCompanyId,
            [FromForm] string pass,
            [FromForm] string product,
            [FromForm] string token
            )
        {
            try
            {
                if (!string.IsNullOrEmpty(configuration["googleReCaptcha:SiteKey"]))
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        throw new Exception("Please provide captcha");
                    }

                    var validation = await captchaValidator.IsCaptchaPassedAsync(token);
                    if (!validation)
                    {
                        throw new Exception("Please provide valid captcha");
                    }
                }
                var place = await placeRepository.GetPlace(chosenPlaceId);
                if (place == null) throw new Exception("Place not found");
                if (string.IsNullOrEmpty(place.PlaceProviderId)) throw new Exception("Place provider missing");
                var pp = await placeProviderRepository.GetPlaceProvider(place.PlaceProviderId);
                if (pp == null) throw new Exception("Place provider missing");


                var visitor = new Visitor()
                {
                    ChosenPlaceId = chosenPlaceId,
                    ChosenSlot = chosenSlotId
                };

                var regId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, personCompanyId));
                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null) throw new Exception("Zadajte platné číslo zamestnanca aj posledné štyri číslice z rodného čísla");
                if (reg.PersonType == "foreign")
                {
                    if (pass.Length < 4 || !reg.Passport.EndsWith(pass)) throw new Exception("Zadajte platné číslo zamestnanca aj posledné štyri číslice z rodného čísla");
                }
                else
                {
                    if (pass.Length < 4 || !reg.RC.EndsWith(pass)) throw new Exception("Zadajte platné číslo zamestnanca aj posledné štyri číslice z rodného čísla");
                }

                visitor.FirstName = reg.FirstName;
                visitor.LastName = reg.LastName;
                visitor.BirthDayDay = reg.BirthDayDay;
                visitor.BirthDayMonth = reg.BirthDayMonth;
                visitor.BirthDayYear = reg.BirthDayYear;
                visitor.City = reg.City;
                visitor.Street = reg.Street;
                visitor.StreetNo = reg.StreetNo;
                visitor.ZIP = reg.ZIP;
                visitor.Email = reg.Email;
                visitor.Phone = reg.Phone;
                visitor.PersonType = reg.PersonType;
                visitor.Passport = reg.Passport;
                visitor.RC = reg.RC;
                visitor.Product = product;


                var time = new DateTimeOffset(visitor.ChosenSlot, TimeSpan.Zero);
                if (time.AddMinutes(10) < DateTimeOffset.Now)
                {
                    throw new Exception("Na tento termín sa nedá zaregistrovať pretože časový úsek je už ukončený");
                }
                if (string.IsNullOrEmpty(visitor.Address))
                {
                    visitor.Address = $"{visitor.Street} {visitor.StreetNo}, {visitor.ZIP} {visitor.City}";
                }

                visitor.RegistrationTime = DateTimeOffset.UtcNow;
                visitor.SelfRegistration = true;
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

                if (!visitor.RegistrationTime.HasValue)
                {
                    visitor.RegistrationTime = DateTimeOffset.UtcNow;
                    visitor.SelfRegistration = false;
                }
                visitor.RegistrationUpdatedByManager = User.GetEmail();
                logger.LogInformation($"RegisterByManager: {User.GetEmail()} {Helpers.Hash.GetSHA256Hash(visitor.Id.ToString())}");
                return Ok(await visitorRepository.Register(visitor, User.GetEmail()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Returns ECIES public key for private data encryption
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        [HttpGet("GetPublicKey")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<string>> GetPublicKey()
        {
            try
            {
                return configuration["ECIES-Public"];
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Tester is allowed to fetch the private key for the encrypted QR code with sensitive data to be decrypted
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetPrivateKey")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<string>> GetPrivateKey()
        {
            try
            {
                if (!User.IsMedicTester(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only MedicTester role is allowed to fetch the private key");
                }

                return configuration["ECIES-Private"];
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Upload employees - PP Administrator can upload employees
        /// </summary>
        /// <returns></returns>
        [HttpPost("UploadEmployees"), RequestSizeLimit(1000000)]
        public async Task<ActionResult<int>> UploadEmployees()
        {
            try
            {
                if (Request.Form.Files.Count != 1) throw new Exception("Please upload file");
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can upload employees");
                }
                var ret = 0;
                var file = Request.Form.Files[0];

                using var stream = new MemoryStream();
                file.CopyTo(stream);

                var outStream = new MemoryStream(stream.ToArray());
                using TextFieldParser csvParser = new TextFieldParser(outStream);
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { ";" });
                csvParser.HasFieldsEnclosedInQuotes = true;
                var line = 0;
                var n2k = new Dictionary<string, int>();
                while (!csvParser.EndOfData)
                {
                    line++;
                    string[] fields = csvParser.ReadFields();

                    if (line == 1)
                    {
                        for (int i = 0; i < fields.Length; i++)
                        {
                            n2k[fields[i].GenerateSlug()] = i;
                        }
                        continue;
                    }
                    var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());

                    var reg = new Registration()
                    {
                        FirstName = fields[n2k["meno"]],
                        LastName = fields[n2k["priezvisko"]],
                        City = fields[n2k["miesto"]],
                        Phone = fields[n2k["telefonne-cislo"]],
                        RC = fields[n2k["idc"]],
                        StreetNo = fields[n2k["supisne-cislo"]] + "/" + fields[n2k["supisne-cislo"]],
                        Street = fields[n2k["ulica-a-cislo-domu"]],
                        Email = fields[n2k["email"]],
                        ZIP = fields[n2k["postsmercmiesto"]],
                        CompanyIdentifiers = new List<CompanyIdentifier>()
                        {
                            new CompanyIdentifier()
                            {
                                CompanyId = pp.CompanyId,
                                CompanyName = pp.CompanyName,
                                EmployeeId = fields[n2k["osobne-cislo"]]
                            }
                        }
                    };
                    if (DateTimeOffset.TryParse(fields[n2k["datum-narodenia"]], out var date))
                    {
                        reg.BirthDayDay = date.Day;
                        reg.BirthDayMonth = date.Month;
                        reg.BirthDayYear = date.Year;
                    }
                    reg.Phone = reg.Phone?.Replace(" ", "").Replace("/", "") ?? "";
                    if (reg.Phone.Length > 5 && reg.Phone.Length <= 10 && reg.Phone.StartsWith("0"))
                    {
                        reg.Phone = "+421" + reg.Phone.Substring(1);
                    }

                    var oldId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, fields[n2k["osobne-cislo"]]));
                    var old = await visitorRepository.GetRegistration(oldId);
                    if (old == null)
                    {
                        reg.Created = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        reg.Created = old.Created;
                    }

                    reg = await visitorRepository.SetRegistration(reg, false);
                    ret++;
                }

                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
