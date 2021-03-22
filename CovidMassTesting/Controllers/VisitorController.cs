using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using SlugGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                    if (string.IsNullOrEmpty(visitor.Passport))
                    {
                        throw new Exception("Zadajte číslo cestovného dokladu prosím");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(visitor.RC))
                    {
                        throw new Exception("Zadajte rodné číslo prosím");
                    }
                }

                if (string.IsNullOrEmpty(visitor.Street))
                {
                    throw new Exception("Zadajte ulicu trvalého bydliska prosím");
                }

                if (string.IsNullOrEmpty(visitor.StreetNo))
                {
                    throw new Exception("Zadajte číslo domu trvalého bydliska prosím");
                }

                if (string.IsNullOrEmpty(visitor.ZIP))
                {
                    throw new Exception("Zadajte PSČ trvalého bydliska prosím");
                }

                if (string.IsNullOrEmpty(visitor.City))
                {
                    throw new Exception("Zadajte mesto trvalého bydliska prosím");
                }

                if (string.IsNullOrEmpty(visitor.FirstName))
                {
                    throw new Exception("Zadajte svoje meno prosím");
                }

                if (string.IsNullOrEmpty(visitor.LastName))
                {
                    throw new Exception("Zadajte svoje priezvisko prosím");
                }

                if (!visitor.BirthDayYear.HasValue || visitor.BirthDayYear < 1900 || visitor.BirthDayYear > 2021)
                {
                    throw new Exception("Rok Vášho narodenia vyzerá byť chybne vyplnený");
                }

                if (!visitor.BirthDayDay.HasValue || visitor.BirthDayDay < 1 || visitor.BirthDayDay > 31)
                {
                    throw new Exception("Deň Vášho narodenia vyzerá byť chybne vyplnený");
                }

                if (!visitor.BirthDayMonth.HasValue || visitor.BirthDayMonth < 1 || visitor.BirthDayMonth > 12)
                {
                    throw new Exception("Mesiac Vášho narodenia vyzerá byť chybne vyplnený");
                }

                visitor.RegistrationTime = DateTimeOffset.UtcNow;
                visitor.SelfRegistration = true;

                var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                if (place == null)
                {
                    throw new Exception("Vybrané miesto nebolo nájdené");
                }
                visitor.PlaceProviderId = place.PlaceProviderId;
                PlaceProduct placeProduct = null;
                try
                {
                    placeProduct = await placeRepository.GetPlaceProduct(visitor.Product);
                }
                catch { }
                Product product = null;
                try
                {
                    if (placeProduct == null)
                    {
                        product = await placeProviderRepository.GetProduct(place.PlaceProviderId, visitor.Product);
                    }
                }
                catch { }

                if (product == null && placeProduct == null)
                {
                    throw new Exception("Vybraná služba nebola nájdená");
                }

                //logger.LogInformation($"EmployeesRegistration: {product.EmployeesRegistration}");
                if (placeProduct?.EmployeesRegistration == true || product?.EmployeesRegistration == true)
                {
                    logger.LogInformation($"EmployeesRegistration 2: {visitor.EmployeeId}");
                    if (string.IsNullOrEmpty(visitor.EmployeeId))
                    {
                        throw new Exception("Zadajte prosím osobné číslo zamestnanca");
                    }

                    var pp = await placeProviderRepository.GetPlaceProvider(place.PlaceProviderId);
                    if (pp == null)
                    {
                        throw new Exception("Miesto má nastavené chybnú spoločnosť. Prosím kontaktujte podporu s chybou 0x021561");
                    }

                    var hash = visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, visitor.EmployeeId);
                    var regId = await visitorRepository.GetRegistrationIdFromHashedId(hash);
                    var reg = await visitorRepository.GetRegistration(regId);
                    logger.LogInformation($"EmployeesRegistration 3: {hash} {regId} {reg?.Id}");
                    if (reg == null)
                    {
                        throw new Exception("Zadajte prosím platné osobné číslo zamestnanca");
                    }

                    var rc = reg.RC ?? "";
                    if (rc.Length > 4)
                    {
                        rc = rc.Substring(rc.Length - 4);
                    }

                    logger.LogInformation($"EmployeesRegistration 4: {rc}");
                    if (string.IsNullOrEmpty(visitor.RC) || !visitor.RC.EndsWith(rc))
                    {
                        throw new Exception("Časť poskytnutého rodného čísla od zamestnávateľa vyzerá byť rozdielna od čísla zadaného v registračnom formulári");
                    }
                }
                return Ok(await visitorRepository.Register(visitor, "", true));
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
        /// Public method for pre registration of company workers.
        /// </summary>
        /// <returns></returns>
        [HttpPost("RegisterWithCompanyRegistration")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> RegisterWithCompanyRegistration(
            [FromForm] long chosenSlot,
            [FromForm] string chosenPlaceId,
            [FromForm] string employeeNumber,
            [FromForm] string pass,
            [FromForm] string product,
            [FromForm] string token
            )
        {
            try
            {
                if (string.IsNullOrEmpty(pass))
                {
                    throw new Exception("Zadajte posledné štyri číslice z rodného čísla");
                }
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
                if (place == null)
                {
                    throw new Exception("Place not found");
                }

                if (string.IsNullOrEmpty(place.PlaceProviderId))
                {
                    throw new Exception("Place provider missing");
                }

                var pp = await placeProviderRepository.GetPlaceProvider(place.PlaceProviderId);
                if (pp == null)
                {
                    throw new Exception("Place provider missing");
                }

                var visitor = new Visitor()
                {
                    ChosenPlaceId = chosenPlaceId,
                    ChosenSlot = chosenSlot
                };

                var regId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, employeeNumber));
                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null)
                {
                    throw new Exception("Zadajte platné osobné číslo zamestnanca aj posledné štyri číslice z rodného čísla");
                }

                if (reg.PersonType == "foreign")
                {
                    if (string.IsNullOrEmpty(reg.RC))
                    {
                        throw new Exception("Vaša registrácia nemá správne vyplnené číslo pasu");
                    }
                    if (pass.Length < 4 || !reg.Passport.EndsWith(pass))
                    {
                        throw new Exception("Zadajte platné osobné číslo zamestnanca aj posledné štyri číslice z rodného čísla");
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(reg.RC))
                    {
                        throw new Exception("Vaša registrácia nemá správne vyplnené rodné číslo");
                    }
                    if (pass.Length < 4 || !reg.RC.EndsWith(pass))
                    {
                        throw new Exception("Zadajte platné osobné číslo zamestnanca aj posledné štyri číslice z rodného čísla");
                    }
                }
                visitor.EmployeeId = reg.CompanyIdentifiers?.Where(c => !string.IsNullOrEmpty(c.EmployeeId))?.FirstOrDefault()?.EmployeeId;
                visitor.PersonType = string.IsNullOrEmpty(reg.PersonType) ? "idcard" : reg.PersonType;
                visitor.FirstName = reg.FirstName;
                visitor.LastName = reg.LastName;
                visitor.BirthDayDay = reg.BirthDayDay;
                visitor.BirthDayMonth = reg.BirthDayMonth;
                visitor.BirthDayYear = reg.BirthDayYear;
                visitor.City = reg.City;
                visitor.Street = reg.Street;
                visitor.StreetNo = reg.StreetNo;
                visitor.Insurance = reg.InsuranceCompany;
                visitor.Gender = reg.Gender;
                visitor.Nationality = reg.Nationality;
                visitor.ZIP = reg.ZIP;
                visitor.Email = reg.CustomEmail ?? reg.Email;
                visitor.Phone = reg.CustomPhone ?? reg.Phone;
                visitor.PersonType = reg.PersonType;
                visitor.Passport = reg.Passport;
                visitor.RC = reg.RC;
                visitor.Product = product;
                visitor.PlaceProviderId = pp.PlaceProviderId;

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
                return Ok(await visitorRepository.Register(visitor, "", true));
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
        /// Registration manager can register visitor with employee number
        /// </summary>
        /// <param name="employeeNumber"></param>
        /// <param name="product"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterEmployeeByManager")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> RegisterEmployeeByManager(
            [FromForm] string employeeNumber,
            [FromForm] string product
        )
        {
            try
            {
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository)
                    && !User.IsMedicTester(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to register user at the place");
                }

                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                if (pp == null)
                {
                    throw new Exception("Place provider missing");
                }

                var visitor = new Visitor()
                {
                };

                var regId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, employeeNumber));
                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null)
                {
                    throw new Exception("Zadajte platné osobné číslo zamestnanca");
                }

                visitor.EmployeeId = reg.CompanyIdentifiers?.Where(c => !string.IsNullOrEmpty(c.EmployeeId))?.FirstOrDefault()?.EmployeeId;
                visitor.FirstName = reg.FirstName;
                visitor.LastName = reg.LastName;
                visitor.BirthDayDay = reg.BirthDayDay;
                visitor.BirthDayMonth = reg.BirthDayMonth;
                visitor.BirthDayYear = reg.BirthDayYear;
                visitor.City = reg.City;
                visitor.Street = reg.Street;
                visitor.StreetNo = reg.StreetNo;
                visitor.ZIP = reg.ZIP;
                visitor.Email = reg.CustomEmail ?? reg.Email;

                visitor.Phone = reg.CustomPhone ?? reg.Phone;

                visitor.PersonType = reg.PersonType;
                visitor.Passport = reg.Passport;
                visitor.RC = reg.RC;
                visitor.Product = product;
                visitor.RegistrationTime = DateTimeOffset.UtcNow;
                visitor.SelfRegistration = false;
                visitor.Gender = reg.Gender;
                visitor.Nationality = reg.Nationality;
                visitor.Insurance = reg.InsuranceCompany;
                visitor.PlaceProviderId = User.GetPlaceProvider();

                visitor.RegistrationUpdatedByManager = User.GetEmail();
                logger.LogInformation($"RegisterEmployeeByManager: {User.GetEmail()} {Helpers.Hash.GetSHA256Hash(visitor.Id.ToString())}");
                return Ok(await visitorRepository.Register(visitor, User.GetEmail(), true));
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
        /// LoadVisitor information by tester with personal number
        /// </summary>
        /// <param name="employeeNumber"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("LoadVisitorByEmployeeNumber")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> LoadVisitorByEmployeeNumber(
            [FromForm] string employeeNumber
        )
        {
            try
            {
                if (!User.IsRegistrationManager(userRepository, placeProviderRepository)
                    && !User.IsMedicTester(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to register user at the place");
                }

                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                if (pp == null)
                {
                    throw new Exception("Place provider missing");
                }

                var regId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, employeeNumber));
                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null)
                {
                    throw new Exception("Zadajte platné osobné číslo zamestnanca");
                }
                var ret = await visitorRepository.GetVisitorByPersonalNumber(reg.RC, true);
                if (ret == null)
                {
                    throw new Exception("Zadajte platné osobné číslo zamestnanca");
                }
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
        /// Load Employee information by documenter
        /// </summary>
        /// <param name="employeeNumber"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("LoadEmployeeByEmployeeNumber")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Registration>> LoadEmployeeByEmployeeNumber(
            [FromForm] string employeeNumber
        )
        {
            try
            {
                if (!User.IsDocumentManager(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only user with Document Manager role is allowed to fetch registration data");
                }

                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                if (pp == null)
                {
                    throw new Exception("Place provider missing");
                }

                var regId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, employeeNumber));
                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null)
                {
                    throw new Exception("Zadajte platné osobné číslo zamestnanca");
                }
                return Ok(reg);
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
                {
                    throw new Exception("Only user with Registration Manager role or Medic Tester role is allowed to register user at the place");
                }

                if (!visitor.RegistrationTime.HasValue)
                {
                    visitor.RegistrationTime = DateTimeOffset.UtcNow;
                    visitor.SelfRegistration = false;
                }
                visitor.RegistrationUpdatedByManager = User.GetEmail();
                visitor.PlaceProviderId = User.GetPlaceProvider();
                logger.LogInformation($"RegisterByManager: {User.GetEmail()} {Helpers.Hash.GetSHA256Hash(visitor.Id.ToString())}");
                return Ok(await visitorRepository.Register(visitor, User.GetEmail(), true));
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
        /// Register employee by the documenter
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="email"></param>
        /// <param name="phone"></param>
        /// <param name="time"></param>
        /// <param name="productId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterEmployeeByDocumenter")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> RegisterEmployeeByDocumenter(
            [FromForm] string employeeId,
            [FromForm] string email,
            [FromForm] string phone,
            [FromForm] DateTimeOffset time,
            [FromForm] string productId,
            [FromForm] string result
            )
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId))
                {
                    throw new ArgumentException($"'{nameof(employeeId)}' cannot be null or empty", nameof(employeeId));
                }

                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException($"'{nameof(email)}' cannot be null or empty", nameof(email));
                }

                if (string.IsNullOrEmpty(phone))
                {
                    throw new ArgumentException($"'{nameof(phone)}' cannot be null or empty", nameof(phone));
                }

                if (string.IsNullOrEmpty(productId))
                {
                    throw new ArgumentException($"'{nameof(productId)}' cannot be null or empty", nameof(productId));
                }

                if (result != TestResult.PositiveWaitingForCertificate && result != TestResult.NegativeWaitingForCertificate)
                {
                    throw new ArgumentException($"'{nameof(result)}' must be positive or negative", nameof(result));
                }

                if (!User.IsDocumentManager(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only user with Document Manager role is allowed to register self tests and external tests");
                }
                if (time > DateTimeOffset.Now.AddMinutes(5))
                {
                    throw new Exception("Uskutočnený čas testu musí byť v minulosti");
                }

                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                if (pp == null)
                {
                    throw new Exception("Miesto má nastavené chybnú spoločnosť. Prosím kontaktujte podporu s chybou 0x027561");
                }
                var hash = visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, employeeId);
                var regId = await visitorRepository.GetRegistrationIdFromHashedId(hash);

                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null)
                {
                    throw new ArgumentException($"Neplatné osobné číslo zamestnanca");
                }
                var phoneFormatted = phone.FormatPhone();
                var regUpdated = false;
                if (phoneFormatted.IsValidPhoneNumber())
                {
                    if (string.IsNullOrEmpty(reg.CustomPhone))
                    {
                        if (reg.Phone != phoneFormatted)
                        {
                            reg.CustomPhone = phoneFormatted;
                            regUpdated = true;
                        }
                    }
                    else
                    {
                        if (reg.CustomPhone != phoneFormatted)
                        {
                            reg.CustomPhone = phoneFormatted;
                            regUpdated = true;
                        }
                    }
                }

                if (email.IsValidEmail())
                {
                    if (string.IsNullOrEmpty(reg.CustomEmail))
                    {
                        if (reg.Email != email)
                        {
                            reg.CustomEmail = email;
                            regUpdated = true;
                        }
                    }
                    else
                    {
                        if (reg.CustomEmail != email)
                        {
                            reg.CustomEmail = email;
                            regUpdated = true;
                        }
                    }
                }

                if (regUpdated)
                {
                    await visitorRepository.SetRegistration(reg, false);
                }
                var visitor = new Visitor()
                {
                };
                visitor.EmployeeId = reg.CompanyIdentifiers?.Where(c => !string.IsNullOrEmpty(c.EmployeeId))?.FirstOrDefault()?.EmployeeId;
                visitor.Result = TestResult.TestIsBeingProcessing;
                visitor.PersonType = string.IsNullOrEmpty(reg.PersonType) ? "idcard" : reg.PersonType;
                visitor.FirstName = reg.FirstName;
                visitor.LastName = reg.LastName;
                visitor.BirthDayDay = reg.BirthDayDay;
                visitor.BirthDayMonth = reg.BirthDayMonth;
                visitor.BirthDayYear = reg.BirthDayYear;
                visitor.City = reg.City;
                visitor.Street = reg.Street;
                visitor.StreetNo = reg.StreetNo;
                visitor.Insurance = reg.InsuranceCompany;
                visitor.Gender = reg.Gender;
                visitor.Nationality = reg.Nationality;
                visitor.ZIP = reg.ZIP;
                visitor.Email = reg.CustomEmail ?? reg.Email;
                visitor.Phone = reg.CustomPhone ?? reg.Phone;
                visitor.PersonType = reg.PersonType;
                visitor.Passport = reg.Passport;
                visitor.RC = reg.RC;
                visitor.Product = productId;
                visitor.RegistrationTime = DateTimeOffset.UtcNow;
                visitor.SelfRegistration = false;
                visitor.RegistrationUpdatedByManager = User.GetEmail();
                visitor.TestingTime = time;
                visitor.ChosenSlot = time.Ticks;
                visitor.PlaceProviderId = User.GetPlaceProvider();

                logger.LogInformation($"RegisterEmployeeByDocumenter: {User.GetEmail()} {Helpers.Hash.GetSHA256Hash(visitor.Id.ToString())}");
                var saved = await visitorRepository.Register(visitor, User.GetEmail(), false);
                var id = Guid.NewGuid().ToString().FormatDocument();
                await visitorRepository.ConnectVisitorToTest(saved.Id, id, User.GetEmail(), User.GetPlaceProvider(), HttpContext.GetIPAddress());
                return Ok(await visitorRepository.SetTestResult(id, result, true));
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
        /// Tester is allowed to fetch the private key for the encrypted QR code with sensitive data to be decrypted
        /// </summary>
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
        /// Upload employees - PP Administrator can upload employees
        /// </summary>
        /// <returns></returns>
        [HttpPost("UploadEmployees"), RequestSizeLimit(1000000)]
        public async Task<ActionResult<int>> UploadEmployees()
        {
            try
            {
                if (Request.Form.Files.Count != 1)
                {
                    throw new Exception("Please upload file");
                }

                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can upload employees");
                }
                var ret = 0;
                var file = Request.Form.Files[0];

                using var stream = new MemoryStream();
                file.CopyTo(stream);

                var outStream = new MemoryStream(stream.ToArray());
                using var csvParser = new TextFieldParser(outStream)
                {
                    CommentTokens = new string[] { "#" }
                };
                csvParser.SetDelimiters(new string[] { ";" });
                csvParser.HasFieldsEnclosedInQuotes = true;
                var line = 0;
                var n2k = new Dictionary<string, int>();
                while (!csvParser.EndOfData)
                {
                    line++;
                    var fields = csvParser.ReadFields();

                    if (line == 1)
                    {
                        for (var i = 0; i < fields.Length; i++)
                        {
                            n2k[fields[i].GenerateSlug()] = i;
                        }
                        continue;
                    }
                    var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                    logger.LogInformation($"Import: {pp.CompanyId} {fields[n2k["osobne-cislo"]]}");

                    if (n2k.ContainsKey("rodne-cislo"))
                    {
                        n2k["idc"] = n2k["rodne-cislo"];
                    }
                    if (n2k.ContainsKey("dat-nar"))
                    {
                        n2k["datum-narodenia"] = n2k["dat-nar"];
                    }
                    if (n2k.ContainsKey("postsmercmiesto"))
                    {
                        n2k["psc"] = n2k["postsmercmiesto"];
                    }
                    if (n2k.ContainsKey("e-mail"))
                    {
                        n2k["email"] = n2k["e-mail"];
                    }
                    if (n2k.ContainsKey("oc"))
                    {
                        n2k["osobne-cislo"] = n2k["oc"];
                    }

                    var reg = new Registration()
                    {
                        PlaceProviderId = User.GetPlaceProvider(),
                        PersonType = "idcard",
                        FirstName = fields[n2k["meno"]],
                        LastName = fields[n2k["priezvisko"]],
                        City = fields[n2k["miesto"]],
                        Phone = fields[n2k["telefonne-cislo"]],
                        RC = fields[n2k["idc"]],
                        Street = fields[n2k["ulica-a-cislo-domu"]],
                        Email = fields[n2k["email"]],
                        ZIP = fields[n2k["psc"]],
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
                    if (n2k.ContainsKey("insurance"))
                    {
                        reg.InsuranceCompany = fields[n2k["insurance"]];
                    }

                    if (string.IsNullOrEmpty(fields[n2k["supisne-cislo"]]))
                    {
                        reg.StreetNo = fields[n2k["orientacne-cislo"]];
                    }
                    else
                    {
                        reg.StreetNo = fields[n2k["supisne-cislo"]] + "/" + fields[n2k["orientacne-cislo"]];
                    }

                    if (n2k.ContainsKey("gender"))
                    {
                        reg.Gender = fields[n2k["gender"]];
                    }
                    if (n2k.ContainsKey("department"))
                    {
                        reg.Department = fields[n2k["department"]];
                    }
                    if (n2k.ContainsKey("nationality"))
                    {
                        reg.Nationality = fields[n2k["nationality"]];
                    }

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
                        reg.Id = old.Id;
                        reg.Created = old.Created;
                        reg.CustomEmail = old.CustomEmail;
                        reg.CustomPhone = old.CustomPhone;
                    }

                    reg = await visitorRepository.SetRegistration(reg, false);
                    ret++;
                }

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

    }
}
