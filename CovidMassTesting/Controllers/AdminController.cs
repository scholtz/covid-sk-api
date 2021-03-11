//#define UseFixes
using CovidMassTesting.Connectors;
using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Model.Email;
using CovidMassTesting.Model.SMS;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// Administration methods
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IStringLocalizer<AdminController> localizer;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<AdminController> logger;
        private readonly ISlotRepository slotRepository;
        private readonly IPlaceRepository placeRepository;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;
        private readonly IVisitorRepository visitorRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly IMojeEZdravie mojeEZdravie;
        private readonly IEmailSender emailSender;
        private readonly ISMSSender smsSender;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="logger"></param>
        /// <param name="slotRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="mojeEZdravie"></param>
        /// <param name="emailSender"></param>
        /// <param name="smsSender"></param>
        public AdminController(
            IStringLocalizer<AdminController> localizer,
            IConfiguration configuration,
            ILoggerFactory loggerFactory,
            ILogger<AdminController> logger,
            ISlotRepository slotRepository,
            IPlaceRepository placeRepository,
            IUserRepository userRepository,
            IVisitorRepository visitorRepository,
            IPlaceProviderRepository placeProviderRepository,
            IMojeEZdravie mojeEZdravie,
            IEmailSender emailSender,
            ISMSSender smsSender
            )
        {
            this.localizer = localizer;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
            this.slotRepository = slotRepository;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
            this.configuration = configuration;
            this.visitorRepository = visitorRepository;
            this.placeProviderRepository = placeProviderRepository;
            this.mojeEZdravie = mojeEZdravie;
            this.emailSender = emailSender;
            this.smsSender = smsSender;
        }
        /// <summary>
        /// Shows available days per place
        /// </summary>
        /// <param name="testingDay"></param>
        /// <param name="from">Hour from when testing place is open</param>
        /// <param name="until">Hour until when testing place is open. 20 means that 20:05 is closed</param>
        /// <returns>Number of new slots</returns>
        [HttpPost("CheckSlots")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> CheckSlots([FromForm] string testingDay = "2020-10-31T00:00:00+00:00", [FromForm] int from = 9, [FromForm] int until = 20)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                var ret = 0;
                foreach (var item in await placeRepository.ListAll())
                {
                    var time = $"{TimeSpan.FromHours(from)}-{TimeSpan.FromHours(until)}";
                    ret += await slotRepository.CheckSlots(DateTimeOffset.Parse(testingDay, CultureInfo.InvariantCulture).Ticks, item.Id, time);
                }
                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Administrator is allowed to invite other users and set their groups
        /// </summary>
        /// <param name="email"></param>
        /// <param name="name"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        [HttpPost("InviteUser")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> InviteUser([FromForm] string email, [FromForm] string name, [FromForm] string[] roles)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }

                return Ok(await userRepository.Add(new Model.User()
                {
                    Email = email,
                    Name = name,
                    Roles = roles.ToList()
                }, User.GetName(), "", true));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix corrupted stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixStats")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> FixStats([FromForm] string placeId, [FromForm] int stats, [FromForm] string type)
        {
            try
            {
                if (placeId is null)
                {
                    throw new ArgumentNullException(nameof(placeId));
                }

                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }

                var place = await placeRepository.GetPlace(placeId);
                switch (type)
                {
                    case "Registrations":
                        logger.LogInformation($"FIX STATS: {placeId} {type} {place.Registrations} -> {stats}");
                        place.Registrations = stats;
                        await placeRepository.SetPlace(place);
                        break;
                    case "Sick":
                        logger.LogInformation($"FIX STATS: {placeId} {type} {place.Sick} -> {stats}");
                        place.Sick = stats;
                        await placeRepository.SetPlace(place);
                        break;
                    case "Healthy":
                        logger.LogInformation($"FIX STATS: {placeId} {type} {place.Healthy} -> {stats}"); place.Sick = stats;
                        place.Healthy = stats;
                        await placeRepository.SetPlace(place);
                        break;
                    default: throw new Exception("Wrong type");
                }
                return Ok(true);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Fix corrupted stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixPersonPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixPersonPlace([FromForm] string day, [FromForm] string newPlaceId, [FromForm] string user)
        {
            try
            {
                if (string.IsNullOrEmpty(day))
                {
                    throw new ArgumentNullException(nameof(day));
                }
                if (string.IsNullOrEmpty(newPlaceId))
                {
                    throw new ArgumentNullException(nameof(newPlaceId));
                }
                if (string.IsNullOrEmpty(user))
                {
                    throw new ArgumentNullException(nameof(user));
                }

                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }

                return Ok(await visitorRepository.FixPersonPlace(day, newPlaceId, user));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Administrator is allowed to invite other users and set their groups
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost("RemoveUser")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> RemoveUser([FromForm] string email)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_remove_users].Value);
                }

                if (User.GetEmail() == email)
                {
                    throw new Exception(localizer[Controllers_AdminController.You_cannot_remove_yourself].Value);
                }

                var mustKeepUsers = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
                if (mustKeepUsers.Any(u => u.Email == email))
                {
                    throw new Exception(localizer[Controllers_AdminController.This_user_is_protected_by_the_configuration].Value);
                }

                return Ok(await userRepository.Remove(email));
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
        [HttpPost("SendResultToEHealth")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> SendResultToEHealth([FromForm] string visitorId)
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can use this method");
                }
                if (configuration["SendResultsToEHealth"] != "1" && configuration["SendResultsToEHealth"] != "0")
                {
                    throw new Exception("Systém nie je nastavený na odosielanie správ do moje eZdravie");
                }
                logger.LogInformation($"SendResultToEHealth: {User.GetEmail()} is sending to nczi {visitorId}");

                var codeClear = visitorId.FormatBarCode();
                if (codeClear.Length == 9 && int.TryParse(codeClear, out var codeInt))
                {
                    var visitor = await visitorRepository.GetVisitor(codeInt);
                    if (visitor == null) throw new Exception("Visitor not found");
                    var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                    var ret = await mojeEZdravie.SendResultToEHealth(visitor, User.GetPlaceProvider(), placeProviderRepository, configuration);
                    if (ret)
                    {
                        visitor = await visitorRepository.GetVisitor(codeInt);
                        visitor.EHealthNotifiedAt = DateTimeOffset.UtcNow;
                        visitor.ResultNotifiedAt = visitor.EHealthNotifiedAt;
                        await visitorRepository.IncrementStats(StatsType.Notification, visitor.ChosenPlaceId, place.PlaceProviderId, visitor.ResultNotifiedAt.Value);
                        await visitorRepository.SetVisitor(visitor, false);
                        logger.LogInformation($"Visitor notified by eHealth {visitor.Id} {visitor.RC.GetSHA256Hash()}");
                    }
                    return Ok(ret);
                }
                throw new Exception("Visitor not found");
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
        /// This method imports all visitor to eHealth which has not yet been sent to eHealth 
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SendDayResultsToEHealth")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> SendDayResultsToEHealth([FromForm] DateTimeOffset date)
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can use this method");
                }
                if (configuration["SendResultsToEHealth"] != "1" && configuration["SendResultsToEHealth"] != "0")
                {
                    throw new Exception("Systém nie je nastavený na odosielanie správ do moje eZdravie");
                }
                logger.LogInformation($"SendDayResultsToEHealth: {User.GetEmail()} is sending to nczi {date}");

                var visitors = await visitorRepository.ListTestedVisitors(date);
                var places = (await placeRepository.ListAll()).Where(place => place.PlaceProviderId == User.GetPlaceProvider()).Select(p => p.Id).ToHashSet();
                visitors = visitors.Where(p => places.Contains(p.ChosenPlaceId));
                int ret = 0;
                foreach (var visitor in visitors)
                {
                    try
                    {
                        if (visitor.EHealthNotifiedAt.HasValue) continue;
                        if (string.IsNullOrEmpty(visitor.RC)) continue;

                        switch (visitor.Result)
                        {
                            case TestResult.PositiveWaitingForCertificate:
                            case TestResult.PositiveCertificateTaken:
                            case TestResult.NegativeWaitingForCertificate:
                            case TestResult.NegativeCertificateTaken:
                                // ok
                                break;
                            default:
                                continue;
                        }

                        var status = await mojeEZdravie.SendResultToEHealth(visitor, User.GetPlaceProvider(), placeProviderRepository, configuration);
                        if (status)
                        {
                            var toUpdate = await visitorRepository.GetVisitor(visitor.Id);
                            toUpdate.EHealthNotifiedAt = DateTimeOffset.UtcNow;
                            toUpdate.ResultNotifiedAt = toUpdate.EHealthNotifiedAt;
                            var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                            await visitorRepository.IncrementStats(StatsType.Notification, visitor.ChosenPlaceId, place.PlaceProviderId, toUpdate.ResultNotifiedAt.Value);
                            await visitorRepository.SetVisitor(toUpdate, false);
                            logger.LogInformation($"Visitor notified by eHealth {toUpdate.Id} {toUpdate.RC.GetSHA256Hash()}");
                            ret++;
                        }
                        else
                        {
                            logger.LogError($"Visitor NOT notified by eHealth {visitor.Id} {visitor.RC.GetSHA256Hash()}");
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, "eHealth: Error while sending data to eHealth: " + exc.Message);
                    }
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
        /// This method exports all visitors who are in state in processing
        /// 
        /// If day is not filled in, use current day
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DownloadEHealthVisitors")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DownloadEHealthVisitors([FromForm] DateTimeOffset? day)
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can use this method");
                }
                if (configuration["SendResultsToEHealth"] != "1" && configuration["SendResultsToEHealth"] != "0")
                {
                    throw new Exception("Systém nie je nastavený na odosielanie správ do moje eZdravie");
                }
                if (!day.HasValue) day = DateTimeOffset.Now;
                logger.LogInformation($"DownloadEHealthVisitors: {User.GetEmail()} {day}");


                return Ok(await mojeEZdravie.DownloadEHealthVisitors(User.GetPlaceProvider(), User.GetEmail(), day.Value, visitorRepository, placeRepository, placeProviderRepository, slotRepository, loggerFactory));
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
        [HttpPost("FindVisitor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> FindVisitor([FromForm] string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    throw new ArgumentException($"'{nameof(query)}' cannot be null or empty", nameof(query));
                }
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can search for visitor");
                }
                logger.LogInformation($"UpdateVisitor: {User.GetEmail()} is fetching visitor {query.GetSHA256Hash()}");

                var codeClear = query.FormatBarCode();
                Visitor ret;
                if (codeClear.Length == 9 && int.TryParse(codeClear, out var codeInt))
                {
                    ret = await visitorRepository.GetVisitor(codeInt);
                    if (ret != null)
                    {
                        logger.LogInformation($"UpdateVisitor: {User.GetEmail()} fetched visitor {ret.Id.ToString().GetSHA256Hash()}");
                        return Ok(ret);
                    }
                }
                var documentClear = query.FormatDocument();
                ret = await visitorRepository.GetVisitorByPersonalNumber(documentClear, true);
                if (ret != null)
                {
                    logger.LogInformation($"UpdateVisitor: {User.GetEmail()} fetched visitor {ret.Id.ToString().GetSHA256Hash()}");
                    return Ok(ret);
                }
                throw new Exception("Visitor not found");
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
        /// Global admin can fix visitor
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        [HttpPost("UpdateVisitor")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> UpdateVisitor([FromBody] Visitor visitor)
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can update visitor directly");
                }

                logger.LogInformation($"UpdateVisitor: {User.GetEmail()} is updating visitor {visitor.Id}");
                return Ok(await visitorRepository.SetVisitor(visitor, false));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Test sms
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        [HttpPost("SendSMS")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> SendSMS([FromQuery] string phone)
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can update visitor directly");
                }

                logger.LogInformation($"SendSMS: {User.GetEmail()} is sending test sms to {phone}");

                return Ok(await smsSender.SendSMS(phone, new Message($"Test sms: {DateTimeOffset.Now.ToString("f")}")));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        [HttpPost("DropDatabase")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> DropDatabase([FromForm] string hash)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_drop_database].Value);
                }

                var drop = await userRepository.DropDatabaseAuthorize(User.GetEmail(), hash);
                if (!drop)
                {
                    throw new Exception(localizer[Controllers_AdminController.Invalid_user_or_password].Value);
                }

                var ret = 0;
                ret += await placeRepository.DropAllData();
                ret += await slotRepository.DropAllData();
                ret += await visitorRepository.DropAllData();
                ret += await userRepository.DropAllData();
                ret += await placeProviderRepository.DropAllData();

                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /*
        [HttpPost("FixDates")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixDates()
        {
            try
            {
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                return Ok(await visitorRepository.FixBirthYear());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /**/
        /// <summary>
        /// Fix stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixStatsAuto")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixStatsAuto()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                return Ok(await visitorRepository.FixStats());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /*
        /// <summary>
        /// Fix stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixVisitorRC")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixVisitorRC()
        {
            try
            {
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                return Ok(await visitorRepository.FixVisitorRC());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /**/
        /// <summary>
        /// Fix stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixTestingTime")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixTestingTime()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                return Ok(await visitorRepository.FixTestingTime());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixVisitorPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Visitor>> FixVisitorPlace([FromForm] int visitorId, [FromForm] string placeId)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"FixVisitorPlace {visitorId} {placeId}");

                var visitor = await visitorRepository.GetVisitor(visitorId);
                if (visitor == null)
                {
                    throw new Exception("Visitor not found");
                }

                visitor.ChosenPlaceId = placeId;

                return Ok(await visitorRepository.SetVisitor(visitor, false));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix verification data
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixVerificationData")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixVerificationData()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"FixVerificationData");

                return Ok(await visitorRepository.FixVerificationData());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix verification data
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixSendRegistrationSMS")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixSendRegistrationSMS()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"FixSendRegistrationSMS");

                return Ok(await visitorRepository.FixSendRegistrationSMS());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix verification data
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixMapVisitorToDay")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixMapVisitorToDay()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"FixMapVisitorToDay");

                return Ok(await visitorRepository.FixMapVisitorToDay());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix verification data
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetVisitorCodeFromTestCode")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> GetVisitorCodeFromTestCode([FromForm] string testingCode)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"GetVisitorCodeFromTestCode by {User.GetEmail()} {testingCode}");
                return Ok(await visitorRepository.GETVisitorCodeFromTesting(testingCode));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Send generic email
        /// </summary>
        /// <returns></returns>
        [HttpPost("SendEmail")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> SendEmail(
            [FromForm] string sendTo,
            [FromForm] DateTimeOffset? from,
            [FromForm] DateTimeOffset? until,
            [FromForm] string subjectSK,
            [FromForm] string subjectCS,
            [FromForm] string subjectEN,
            [FromForm] string textSK,
            [FromForm] string textCS,
            [FromForm] string textEN
            )
        {
            try
            {
                int ret = 0;
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }
                if (string.IsNullOrEmpty(textEN))
                {
                    textEN = textSK;
                }
                if (string.IsNullOrEmpty(textSK))
                {
                    textSK = textEN;
                }
                if (string.IsNullOrEmpty(textCS))
                {
                    textCS = textSK;
                }
                var subject = subjectSK;
                if (CultureInfo.CurrentCulture.Name.StartsWith("en"))
                {
                    subject = subjectEN;
                }
                if (CultureInfo.CurrentCulture.Name.StartsWith("cs"))
                {
                    subject = subjectCS;
                }
                var email = new GenericEmail(CultureInfo.CurrentCulture.Name, configuration["FrontedURL"], configuration["EmailSupport"], configuration["PhoneSupport"])
                {
                    TextSK = textSK,
                    TextCS = textCS,
                    TextEN = textEN,
                    SubjectCS = subjectCS,
                    SubjectEN = subjectEN,
                    SubjectSK = subjectSK
                };

                if (sendTo == "test")
                {
                    await emailSender.SendEmail(subject, "ludovit@scholtz.sk", "Scholtz", email);
                    ret++;
                }
                else if (sendTo == "eHealth")
                {
                    var oldCulture = CultureInfo.CurrentCulture;
                    var oldUICulture = CultureInfo.CurrentUICulture;
                    foreach (var visitor in await visitorRepository.ListTestedVisitors())
                    {
                        if (string.IsNullOrEmpty(visitor.Email)) continue;
                        if (from.HasValue && visitor.TestingTime < from.Value) continue;
                        if (until.HasValue && visitor.TestingTime > until.Value) continue;

                        // >>>>>>>>>>>>>>>>>>>>>>>>>>>>
                        if (!visitor.EHealthNotifiedAt.HasValue) continue;

                        var specifiedCulture = new CultureInfo(visitor.Language ?? "en");
                        CultureInfo.CurrentCulture = specifiedCulture;
                        CultureInfo.CurrentUICulture = specifiedCulture;

                        subject = subjectSK;
                        if (CultureInfo.CurrentCulture.Name.StartsWith("en"))
                        {
                            subject = subjectEN;
                        }
                        if (CultureInfo.CurrentCulture.Name.StartsWith("cs"))
                        {
                            subject = subjectCS;
                        }

                        await emailSender.SendEmail(subject, visitor.Email, $"{visitor.FirstName} {visitor.LastName}", email);
                        logger.LogInformation($"SendEmailGeneric: Sent to {visitor.Id}");
                        ret++;
                    }
                    CultureInfo.CurrentCulture = oldCulture;
                    CultureInfo.CurrentUICulture = oldUICulture;
                }
                else
                {
                    await emailSender.SendEmail(subject, sendTo, "Scholtz", email);
                    ret++;
                }
                return ret;
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }






#if UseFixes
        [HttpPost("Fix01")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> Fix01()
        {
            try
            {
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                return Ok(visitorRepository.Fix01());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Send your registration is not valid to all registered visitors. Some real people were registered to demo backend.
        /// </summary>
        /// <returns></returns>
        [HttpPost("Fix02")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> Fix02()
        {
            try
            {
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                return Ok(visitorRepository.Fix02());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Send your registration is not valid to all registered visitors. Some real people were registered to demo backend.
        /// </summary>
        /// <returns></returns>
        [HttpPost("Fix03")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> Fix03()
        {
            try
            {
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                return Ok(visitorRepository.Fix03());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
#endif
    }
}
