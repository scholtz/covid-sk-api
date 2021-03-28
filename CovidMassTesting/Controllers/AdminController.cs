//#define UseFixes
using CovidMassTesting.Connectors;
using CovidMassTesting.Controllers.Email;
using CovidMassTesting.Controllers.SMS;
using CovidMassTesting.Helpers;
using CovidMassTesting.Model;
using CovidMassTesting.Model.Email;
using CovidMassTesting.Model.Enums;
using CovidMassTesting.Model.SMS;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
                    ret += await slotRepository.CheckSlots(DateTimeOffset.Parse(testingDay, CultureInfo.InvariantCulture).UtcTicks, item.Id, time);
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
                if (place == null) throw new Exception("Place must not be null");
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
        /// DashboardStats
        /// </summary>
        /// <returns></returns>
        [HttpGet("DashboardStats")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Model.Charts.Dashboard>> DashboardStats()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                var enot = await visitorRepository.GetPPStats(StatsType.EHealthNotification, User.GetPlaceProvider());
                var rTo = await visitorRepository.GetPPStats(StatsType.RegisteredOn, User.GetPlaceProvider());
                var rOn = await visitorRepository.GetPPStats(StatsType.RegisteredOn, User.GetPlaceProvider());
                var pos = await visitorRepository.GetPPStats(StatsType.Positive, User.GetPlaceProvider());
                var neg = await visitorRepository.GetPPStats(StatsType.Negative, User.GetPlaceProvider());
                var tested = await visitorRepository.GetPPStats(StatsType.Tested, User.GetPlaceProvider());

                var stats = new SortedDictionary<DateTimeOffset, Dictionary<string, long>>();
                foreach (var item in enot)
                {
                    if (!stats.ContainsKey(item.Key.Date)) stats[item.Key.Date] = new Dictionary<string, long>();
                    stats[item.Key.Date]["enot"] = item.Value;
                }
                foreach (var item in rTo)
                {
                    if (!stats.ContainsKey(item.Key.Date)) stats[item.Key.Date] = new Dictionary<string, long>();
                    stats[item.Key.Date]["rTo"] = item.Value;
                }
                foreach (var item in rOn)
                {
                    if (!stats.ContainsKey(item.Key.Date)) stats[item.Key.Date] = new Dictionary<string, long>();
                    stats[item.Key.Date]["rOn"] = item.Value;
                }
                foreach (var item in pos)
                {
                    if (!stats.ContainsKey(item.Key.Date)) stats[item.Key.Date] = new Dictionary<string, long>();
                    stats[item.Key.Date]["positive"] = item.Value;
                }
                foreach (var item in neg)
                {
                    if (!stats.ContainsKey(item.Key.Date)) stats[item.Key.Date] = new Dictionary<string, long>();
                    stats[item.Key.Date]["negative"] = item.Value;
                }
                foreach (var item in tested)
                {
                    if (!stats.ContainsKey(item.Key.Date)) stats[item.Key.Date] = new Dictionary<string, long>();
                    stats[item.Key.Date]["tested"] = item.Value;
                }

                var ret = new Model.Charts.Dashboard()
                {
                    Labels = stats.Keys.Select(k => k.ToString("yyyy-MM-dd")).ToArray(),
                };
                /*
                ret.Series.Add(new Model.Charts.ChartSeries()
                {
                    Name = "enot",
                    Data = stats.OrderBy(k => k.Key).Select(v =>
                    {
                        if (v.Value.ContainsKey("enot"))
                        {
                            return v.Value["enot"];
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToArray()
                });

                ret.Series.Add(new Model.Charts.ChartSeries()
                {
                    Name = "rTo",
                    Data = stats.OrderBy(k => k.Key).Select(v =>
                    {
                        if (v.Value.ContainsKey("rTo"))
                        {
                            return v.Value["rTo"];
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToArray()
                });
                ret.Series.Add(new Model.Charts.ChartSeries()
                {
                    Name = "rOn",
                    Data = stats.OrderBy(k => k.Key).Select(v =>
                    {
                        if (v.Value.ContainsKey("rOn"))
                        {
                            return v.Value["rOn"];
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToArray()
                });
                /**/
                ret.Series.Add(new Model.Charts.ChartSeries()
                {
                    Name = "positive",
                    Data = stats.Select(v =>
                    {
                        if (v.Value.ContainsKey("positive"))
                        {
                            return v.Value["positive"];
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToArray()
                });
                ret.Series.Add(new Model.Charts.ChartSeries()
                {
                    Name = "negative",
                    Data = stats.Select(v =>
                    {
                        if (v.Value.ContainsKey("negative"))
                        {
                            return v.Value["negative"];
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToArray()
                });
                ret.Series.Add(new Model.Charts.ChartSeries()
                {
                    Name = "tested",
                    Data = stats.Select(v =>
                    {
                        if (v.Value.ContainsKey("tested"))
                        {
                            return v.Value["tested"];
                        }
                        else
                        {
                            return 0;
                        }
                    }).ToArray()
                });
                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Fix advanced stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixAdvancedStats")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> FixAdvancedStats([FromForm] DateTimeOffset? from)
        {
            try
            {
                int i = 0;
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation("FixAdvancedStats");
                await visitorRepository.DropAllStats(from);
                var places = await placeRepository.ListAll();
                var visitors = await visitorRepository.ListAllVisitorsOrig(User.GetPlaceProvider());

                foreach (var visitor in visitors)
                {
                    try
                    {
                        var place = places.FirstOrDefault(p => p.Id == visitor.ChosenPlaceId);
                        var pp = visitor.PlaceProviderId ?? place.PlaceProviderId;
                        if (string.IsNullOrEmpty(pp)) continue;
                        if (visitor.EHealthNotifiedAt.HasValue)
                        {
                            if (!from.HasValue || from >= visitor.EHealthNotifiedAt)
                            {
                                await visitorRepository.IncrementStats(StatsType.EHealthNotification, visitor.ChosenPlaceId, pp, visitor.EHealthNotifiedAt.Value);
                            }
                        }
                        if (string.IsNullOrEmpty(pp)) continue;// place was deleted and visitor does not contain pp

                        if (!from.HasValue || from >= visitor.ChosenSlotTime)
                        {
                            await visitorRepository.IncrementStats(StatsType.RegisteredTo, visitor.ChosenPlaceId, pp, visitor.ChosenSlotTime);
                        }
                        var to = visitor.RegistrationTime ?? visitor.ChosenSlotTime;
                        if (!from.HasValue || from >= to)
                        {
                            await visitorRepository.IncrementStats(StatsType.RegisteredOn, visitor.ChosenPlaceId, pp, to);
                        }
                        i++;
                        if (visitor.TestingTime.HasValue)
                        {
                            if (!from.HasValue || from >= visitor.TestingTime.Value)
                            {
                                await visitorRepository.IncrementStats(StatsType.Tested, visitor.ChosenPlaceId, pp, visitor.TestingTime.Value);
                                await visitorRepository.IncrementStats(StatsType.Notification, visitor.ChosenPlaceId, pp, visitor.TestingTime.Value);
                            }
                        }
                        if (visitor.Result == TestResult.PositiveCertificateTaken || visitor.Result == TestResult.PositiveWaitingForCertificate)
                        {
                            if (!from.HasValue || from >= visitor.TestingTime.Value)
                            {
                                await visitorRepository.IncrementStats(StatsType.Positive, visitor.ChosenPlaceId, pp, visitor.TestingTime.Value);
                            }
                        }
                        if (visitor.Result == TestResult.NegativeCertificateTaken || visitor.Result == TestResult.NegativeWaitingForCertificate)
                        {
                            if (!from.HasValue || from >= visitor.TestingTime.Value)
                            {
                                await visitorRepository.IncrementStats(StatsType.Negative, visitor.ChosenPlaceId, pp, visitor.TestingTime.Value);
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, $"FixAdvancedStats error {exc.Message}");
                    }
                }
                logger.LogInformation($"FixAdvancedStats done {i}");
                return Ok(i);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// fix slots stats
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        [HttpPost("FixAdvancedStatsSlots")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> FixAdvancedStatsSlots([FromForm] DateTimeOffset? from)
        {
            try
            {
                int i = 0;
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation($"FixAdvancedStatsSlots {from}");
                await slotRepository.DropAllStats(from);
                var places = await placeRepository.ListAll();
                var visitors = await visitorRepository.ListAllVisitorsOrig(User.GetPlaceProvider());

                foreach (var visitor in visitors)
                {
                    try
                    {
                        var place = places.FirstOrDefault(p => p.Id == visitor.ChosenPlaceId);
                        if (place == null) continue;
                        await slotRepository.IncrementStats(StatsType.Enum.RegisteredTo, SlotType.Enum.Min, place.Id, visitor.ChosenSlotTime);
                        await slotRepository.IncrementStats(StatsType.Enum.RegisteredTo, SlotType.Enum.Hour, place.Id, visitor.ChosenSlotTime);
                        await slotRepository.IncrementStats(StatsType.Enum.RegisteredTo, SlotType.Enum.Day, place.Id, visitor.ChosenSlotTime);

                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, $"FixAdvancedStatsSlots error {exc.Message}");
                    }
                }

                i = await slotRepository.FixAllSlots();

                logger.LogInformation($"FixAdvancedStatsSlots done {i}");
                return Ok(i);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// fix slots stats
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixSlotsOnly")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> FixSlotsOnly()
        {
            try
            {
                int i = 0;
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation($"FixSlotsOnly");

                i = await slotRepository.FixAllSlots();

                logger.LogInformation($"FixSlotsOnly done {i}");
                return Ok(i);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Move visitors by one hour
        /// </summary>
        /// <param name="regFrom"></param>
        /// <param name="regUntil"></param>
        /// <returns></returns>
        [HttpPost("FixMoveVisitorsToSummerTime")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> FixMoveVisitorsToSummerTime([FromForm] DateTimeOffset? regFrom, [FromForm] DateTimeOffset? regUntil)
        {
            try
            {
                int i = 0;
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation($"FixMoveVisitorsToSummerTime {regFrom} {regUntil}");
                var places = await placeRepository.ListAll();
                var visitors = await visitorRepository.ListAllVisitorsOrig(User.GetPlaceProvider());
                var decision = DateTimeOffset.Parse("2021-03-28T00:00:00+00:00");

                foreach (var visitor in visitors)
                {
                    try
                    {
                        if (visitor.ChosenSlotTime >= decision)
                        {
                            if (regFrom.HasValue)
                            {
                                if (visitor.RegistrationTime < regFrom) continue;
                            }
                            if (regUntil.HasValue)
                            {
                                if (visitor.RegistrationTime >= regUntil) continue;
                            }

                            var newSlot = visitor.ChosenSlotTime.AddHours(-1).UtcTicks;
                            var slotFound = false;
                            try
                            {
                                var checkSlot = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, newSlot);
                                if (checkSlot != null)
                                {
                                    slotFound = true;
                                }
                            }
                            catch
                            {
                                slotFound = false;
                            }
                            if (slotFound)
                            {
                                visitor.ChosenSlot = newSlot;
                                await visitorRepository.SetVisitor(visitor, false);
                                i++;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, $"FixMoveVisitorsToSummerTime error {visitor.Id} {exc.Message}");
                    }
                }


                logger.LogInformation($"FixMoveVisitorsToSummerTime done {i}");
                return Ok(i);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Some slots at the time change from winter to summer time does not have description properly filled in regarding the timestamp
        /// </summary>
        /// <returns></returns>
        [HttpPost("ReportSlotIssues")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<Slot1Hour>>> ReportSlotIssues()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation($"ReportSlotIssues");
                StringBuilder log = new StringBuilder();
                var ret = new List<Slot1Hour>();
                var places = await placeRepository.ListAll();
                foreach (var place in places)
                {
                    var days = await slotRepository.ListDaySlotsByPlace(place.Id);
                    foreach (var day in days)
                    {
                        if (day.Time < DateTimeOffset.Parse("2021-03-01")) continue;
                        var hours = await slotRepository.ListHourSlotsByPlaceAndDaySlotId(place.Id, day.SlotId);
                        foreach (var hour in hours)
                        {
                            var shouldBe = $"{hour.Time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(hour.Time.AddHours(1).ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}";
                            if (hour.Description != shouldBe)
                            {
                                ret.Add(hour);
                                log.AppendLine($"{hour.PlaceId} {hour.SlotId} {hour.Time.ToString("o")} {hour.TimeInCET.ToString("o")} {hour.Description} != {shouldBe}");
                            }
                        }
                    }
                }
                logger.LogInformation($"ReportSlotIssues done {ret.Count}");
                logger.LogInformation($"ReportSlotIssues {log}");
                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Some slots at the time change from winter to summer time does not have description properly filled in regarding the timestamp
        /// </summary>
        /// <returns></returns>
        [HttpPost("ReportSlotIssuesM")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<Slot1Hour>>> ReportSlotIssuesM()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation($"ReportSlotIssuesM");
                StringBuilder log = new StringBuilder();
                var ret = new List<Slot1Hour>();
                var places = await placeRepository.ListAll();
                foreach (var place in places)
                {
                    var days = await slotRepository.ListDaySlotsByPlace(place.Id);
                    foreach (var day in days)
                    {
                        if (day.Time < DateTimeOffset.Parse("2021-03-01")) continue;
                        var hours = await slotRepository.ListHourSlotsByPlaceAndDaySlotId(place.Id, day.SlotId);
                        foreach (var hour in hours)
                        {
                            var shouldBe = $"{hour.Time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(hour.Time.AddHours(1).ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}";
                            if (hour.Description != shouldBe)
                            {
                                ret.Add(hour);
                                log.AppendLine($"{hour.PlaceId} {hour.SlotId} {hour.Time.ToString("o")} {hour.TimeInCET.ToString("o")} {hour.Description} != {shouldBe}");
                            }
                        }
                    }
                }
                logger.LogInformation($"ReportSlotIssuesM done {ret.Count}");
                logger.LogInformation($"ReportSlotIssuesM {log}");
                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Some slots at the time change from winter to summer time does not have description properly filled in regarding the timestamp
        /// </summary>
        /// <returns></returns>
        [HttpPost("FixSlotIssues")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<object>>> FixSlotIssues([FromForm] bool? rewrite)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                }
                logger.LogInformation($"FixSlotIssues");
                StringBuilder log = new StringBuilder();
                var ret = new List<object>();
                var places = await placeRepository.ListAll();
                foreach (var place in places)
                {
                    var days = await slotRepository.ListDaySlotsByPlace(place.Id);
                    foreach (var day in days)
                    {
                        if (day.Time < DateTimeOffset.Parse("2021-03-01")) continue;
                        var hours = await slotRepository.ListHourSlotsByPlaceAndDaySlotId(place.Id, day.SlotId);
                        foreach (var hour in hours.OrderBy(h => h.SlotId))
                        {
                            var shouldBe = $"{hour.Time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(hour.Time.AddHours(1).ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}";

                            if (hour.Description != shouldBe)
                            {
                                var clone = new Slot1Hour()
                                {
                                    DaySlotId = hour.DaySlotId,
                                    Description = hour.Description,
                                    PlaceId = hour.PlaceId,
                                    Registrations = hour.Registrations,
                                    TestingDayId = hour.TestingDayId,
                                    Time = hour.Time.AddHours(-1)
                                };
                                shouldBe = $"{clone.Time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(clone.Time.AddHours(1).ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}";
                                if (clone.Description == shouldBe)
                                {


                                    var minutes = await slotRepository.ListMinuteSlotsByPlaceAndHourSlotId(place.Id, hour.SlotId);
                                    foreach (var minute in minutes.OrderBy(h => h.SlotId))
                                    {

                                        var shouldBeM = $"{minute.Time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(minute.Time.AddHours(1).ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}";

                                        if (minute.Description != shouldBeM)
                                        {
                                            var cloneM = new Slot5Min()
                                            {
                                                Description = minute.Description,
                                                PlaceId = minute.PlaceId,
                                                Registrations = minute.Registrations,
                                                TestingDayId = minute.TestingDayId,
                                                Time = minute.Time.AddHours(-1),
                                                HourSlotId = clone.SlotId
                                            };
                                            shouldBeM = $"{cloneM.Time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} - {(cloneM.Time.AddMinutes(5).ToLocalTime()).ToString("HH:mm", CultureInfo.CurrentCulture)}";
                                            if (cloneM.Description == shouldBeM)
                                            {
                                                try
                                                {
                                                    var result = await slotRepository.SetMinuteSlot(cloneM, true);
                                                    await slotRepository.DeleteMinuteSlot(minute);
                                                    ret.Add(cloneM);

                                                    log.AppendLine($"OK {cloneM.PlaceId} {cloneM.SlotId} {cloneM.Time.ToString("o")} {cloneM.TimeInCET.ToString("o")} {cloneM.Description} != {shouldBeM} :: ");
                                                }
                                                catch (Exception exc)
                                                {
                                                    logger.LogError(exc, exc.Message);
                                                    log.AppendLine($"FAILED {cloneM.PlaceId} {cloneM.SlotId} {cloneM.Time.ToString("o")} {cloneM.TimeInCET.ToString("o")} {cloneM.Description} != {shouldBeM} :: ");

                                                    if (rewrite == true)
                                                    {
                                                        var result = await slotRepository.SetMinuteSlot(cloneM, false);
                                                        await slotRepository.DeleteMinuteSlot(minute);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    try
                                    {
                                        var result = await slotRepository.SetHourSlot(clone, true);
                                        await slotRepository.DeleteHourSlot(hour);
                                        ret.Add(clone);

                                        log.AppendLine($"OK {clone.PlaceId} {clone.SlotId} {clone.Time.ToString("o")} {clone.TimeInCET.ToString("o")} {clone.Description} != {shouldBe} :: ");
                                    }
                                    catch (Exception exc)
                                    {
                                        logger.LogError(exc, exc.Message);
                                        log.AppendLine($"FAILED {clone.PlaceId} {clone.SlotId} {clone.Time.ToString("o")} {clone.TimeInCET.ToString("o")} {clone.Description} != {shouldBe} :: ");

                                        if (rewrite == true)
                                        {
                                            var result = await slotRepository.SetHourSlot(clone, false);
                                            await slotRepository.DeleteHourSlot(hour);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                logger.LogInformation($"FixSlotIssues done {ret.Count}");
                logger.LogInformation($"FixSlotIssues {log}");
                return Ok(ret);
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
                    var ret = await mojeEZdravie.SendResultToEHealth(visitor, User.GetPlaceProvider(), placeProviderRepository, configuration);
                    if (ret)
                    {
                        visitor = await visitorRepository.GetVisitor(codeInt);
                        visitor.EHealthNotifiedAt = DateTimeOffset.UtcNow;
                        if (!visitor.ResultNotifiedAt.HasValue)
                        {
                            await visitorRepository.IncrementStats(StatsType.Tested, visitor.ChosenPlaceId, visitor.PlaceProviderId, visitor.ResultNotifiedAt.Value);
                        }
                        visitor.ResultNotifiedAt = visitor.EHealthNotifiedAt;
                        await visitorRepository.IncrementStats(StatsType.Notification, visitor.ChosenPlaceId, visitor.PlaceProviderId, visitor.ResultNotifiedAt.Value);
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

                var visitors = await visitorRepository.ListTestedVisitors(User.GetPlaceProvider(), date);
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

                        var status = await mojeEZdravie.SendResultToEHealth(visitor.visitor, User.GetPlaceProvider(), placeProviderRepository, configuration);
                        if (status)
                        {
                            var toUpdate = await visitorRepository.GetVisitor(visitor.Id);
                            toUpdate.EHealthNotifiedAt = DateTimeOffset.UtcNow;
                            if (!visitor.ResultNotifiedAt.HasValue)
                            {
                                await visitorRepository.IncrementStats(StatsType.Tested, visitor.ChosenPlaceId, visitor.PlaceProviderId, visitor.ResultNotifiedAt.Value);
                            }
                            toUpdate.ResultNotifiedAt = toUpdate.EHealthNotifiedAt;
                            await visitorRepository.IncrementStats(StatsType.Notification, visitor.ChosenPlaceId, visitor.PlaceProviderId, toUpdate.ResultNotifiedAt.Value);
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
                logger.LogInformation($"FindVisitor: {User.GetEmail()} is fetching visitor {query.GetSHA256Hash()}");

                var codeClear = query.FormatBarCode();
                Visitor ret;
                if (codeClear.Length == 9 && int.TryParse(codeClear, out var codeInt))
                {
                    ret = await visitorRepository.GetVisitor(codeInt);
                    if (ret != null)
                    {
                        logger.LogInformation($"FindVisitor: {User.GetEmail()} fetched visitor {ret.Id.ToString().GetSHA256Hash()}");

                        try
                        {
                            var places = (await placeRepository.ListAll()).ToDictionary(p => p.Id, p => p);
                            var products = (await placeProviderRepository.ListAll()).SelectMany(p => p.Products).ToDictionary(p => p.Id, p => p);
                            ret.Extend(places, products);
                            logger.LogInformation($"visitor extended: {ret.Id} {ret.ProductName}");
                        }
                        catch (Exception exc)
                        {
                            logger.LogError(exc, $"Error in visitor: {exc.Message}");
                        }


                        return Ok(ret);
                    }
                }
                var documentClear = query.FormatDocument();
                ret = await visitorRepository.GetVisitorByPersonalNumber(documentClear, true);

                if (ret != null)
                {
                    logger.LogInformation($"FindVisitor: {User.GetEmail()} fetched visitor {ret.Id.ToString().GetSHA256Hash()}");

                    return Ok(ret);
                }


                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                if (pp == null)
                {
                    throw new Exception("Place provider missing");
                }
                var regId = await visitorRepository.GetRegistrationIdFromHashedId(visitorRepository.MakeCompanyPeronalNumberHash(pp.CompanyId, query));
                var reg = await visitorRepository.GetRegistration(regId);
                if (reg == null)
                {
                    throw new Exception("Zadajte platné osobné číslo zamestnanca");
                }
                ret = await visitorRepository.GetVisitorByPersonalNumber(reg.RC, true);
                if (ret != null)
                {
                    var places = (await placeRepository.ListAll()).ToDictionary(p => p.Id, p => p);
                    var products = (await placeProviderRepository.ListAll()).SelectMany(p => p.Products).ToDictionary(p => p.Id, p => p);
                    ret.Extend(places, products);
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
        /// Reset password 
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost("ResetPassword")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> ResetPassword([FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));
                }
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can reset user password");
                }

                logger.LogInformation($"ResetPassword by admin: {User.GetEmail()} {email}");
                return Ok(await userRepository.ResetPassword(email, User.GetPlaceProvider()));
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
        /// Test sms
        /// </summary>
        /// <param name="test">Test first</param>
        /// <returns></returns>
        [HttpPost("SendSMSSummerZone")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> SendSMSSummerZone([FromForm] string test, [FromForm] DateTimeOffset day)
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository))
                {
                    throw new Exception("Only administrator can update visitor directly");
                }

                logger.LogInformation($"SendSMSSummerZone: {User.GetEmail()}");

                var days = await visitorRepository.ListExportableDays();

                //var today = days.FirstOrDefault(d => d.UtcTicks >= DateTimeOffset.Now.AddDays(-1).UtcTicks && d.UtcTicks < DateTimeOffset.Now.AddDays(-1).UtcTicks);

                var allVisitors = await visitorRepository.ListAllVisitorsOrig(User.GetPlaceProvider(), day);
                int ret = 0;

                logger.LogInformation($"SendSMSSummerZone: Count all: {allVisitors.Count()} {day}");
                foreach (var visitor in allVisitors)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(visitor.Phone)) continue;
                        if (!string.IsNullOrEmpty(visitor.TestingSet)) continue;
                        var text = "";
                        var range = $"{visitor.ChosenSlotTime.ToLocalTime().ToString("HH:mm")} - {visitor.ChosenSlotTime.AddMinutes(5).ToLocalTime().ToString("HH:mm")}";
                        switch (visitor.ChosenPlaceId)
                        {
                            case "BA602":
                                if (visitor.ChosenSlotTime.UtcTicks >= 637525176000000000L && visitor.ChosenSlotTime.UtcTicks < 637525260000000000L)
                                {
                                    switch (visitor.Language)
                                    {
                                        case "en":
                                        case "en-US":
                                            text = $"{visitor.FirstName} {visitor.LastName}, please visit testing place MU Ruzinov between {range}. Please note, that between 11:40 - 13:00 is lunch break.";
                                            break;
                                        default:
                                            text = $"{visitor.FirstName} {visitor.LastName}, pridte sa dnes prosim otestovat do MU Ruzinov medzi {range}. Pozor, medzi 11:40 - 13:00 je obednajsia prestavka.";
                                            break;
                                    }
                                }
                                if (visitor.ChosenSlotTime.UtcTicks >= 637525392000000000L)
                                {
                                    switch (visitor.Language)
                                    {
                                        case "en":
                                        case "en-US":
                                            text = $"{visitor.FirstName} {visitor.LastName}, please visit testing place MU Ruzinov between {range}. Please note, that after 17:45 place is closed.";
                                            break;
                                        default:
                                            text = $"{visitor.FirstName} {visitor.LastName}, pridte sa dnes prosim otestovat do MU Ruzinov medzi {range}. Pozor, po 17:45 je odberne miesto zatvorene.";
                                            break;
                                    }
                                }
                                break;
                            case "BA601":
                                if (visitor.ChosenSlotTime.UtcTicks >= 637525392000000000L)
                                {
                                    switch (visitor.Language)
                                    {
                                        case "en":
                                        case "en-US":
                                            text = $"{visitor.FirstName} {visitor.LastName}, please visit testing place at the airport between {range}. Please note, that after 17:40 place is closed.";
                                            break;
                                        default:
                                            text = $"{visitor.FirstName} {visitor.LastName}, pridte sa dnes prosim otestovat na letisko medzi {range}. Pozor, po 17:40 je odberne miesto zatvorene.";
                                            break;
                                    }
                                }
                                break;
                            case "62e2943e-ef10-49d7-9243-41d206c3aac8":

                                if (visitor.ChosenSlotTime.UtcTicks >= 637525176000000000L && visitor.ChosenSlotTime.UtcTicks < 637525260000000000L)
                                {
                                    switch (visitor.Language)
                                    {
                                        case "en":
                                        case "en-US":
                                            text = $"{visitor.FirstName} {visitor.LastName}, please visit testing place Strkovec between {range}. Please note, that between 11:45 - 13:00 is lunch break.";
                                            break;
                                        default:
                                            text = $"{visitor.FirstName} {visitor.LastName}, pridte sa dnes prosim otestovat ku Strkovcu medzi {range}. Pozor, medzi 11:45 - 13:00 je obednajsia prestavka.";
                                            break;
                                    }
                                }
                                if (visitor.ChosenSlotTime.UtcTicks >= 637525392000000000L)
                                {
                                    switch (visitor.Language)
                                    {
                                        case "en":
                                        case "en-US":
                                            text = $"{visitor.FirstName} {visitor.LastName}, please visit testing place Strkovec between {range}. Please note, that after 17:45 place is closed.";
                                            break;
                                        default:
                                            text = $"{visitor.FirstName} {visitor.LastName}, pridte sa dnes prosim otestovat ku Strkovcu medzi {range}. Pozor, po 17:45 je odberne miesto zatvorene.";
                                            break;
                                    }
                                }
                                break;
                        }

                        logger.LogInformation($"SendSMSSummerZone: {visitor.Id}");
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (string.IsNullOrEmpty(test))
                            {
                                await smsSender.SendSMS(visitor.Phone, new Message(text));
                                ret++;
                            }
                            else
                            {
                                return Ok(await smsSender.SendSMS(test, new Message(text)));
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, $"SendSMSSummerZone: ERROR {exc.Message}");
                    }
                }
                logger.LogInformation($"SendSMSSummerZone: DONE {ret}");
                return Ok(ret);

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
        /// FixConnectVisitorsWithEmployeeId
        /// </summary>
        /// <returns></returns>

        [HttpPost("FixConnectVisitorsWithEmployeeId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> FixConnectVisitorsWithEmployeeId()
        {
            try
            {
                var ret = 0;
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"FixConnectVisitorsWithEmployeeId");

                var registrations = await visitorRepository.ExportRegistrations(placeProviderId: User.GetPlaceProvider());
                var visitors = await visitorRepository.ListAllVisitorsOrig(User.GetPlaceProvider());
                foreach (var visitor in visitors)
                {
                    var updated = false;
                    var employee = registrations.FirstOrDefault(r => r.RC == visitor.RC);
                    if (employee == null) continue;
                    if (!string.IsNullOrEmpty(employee.Nationality))
                    {
                        if (visitor.Nationality != employee.Nationality)
                        {
                            visitor.Nationality = employee.Nationality;
                            updated = true;
                        }
                    }
                    if (!string.IsNullOrEmpty(employee.LastName))
                    {
                        if (visitor.LastName != employee.LastName)
                        {
                            visitor.LastName = employee.LastName;
                            updated = true;
                        }
                    }
                    var employeeId = employee.CompanyIdentifiers?.Select(r => r.EmployeeId)?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(employeeId))
                    {
                        if (visitor.EmployeeId != employeeId)
                        {
                            visitor.EmployeeId = employeeId;
                            updated = true;
                        }
                    }

                    if (updated)
                    {
                        logger.LogInformation($"Fixing user {visitor.Id}");
                        await visitorRepository.SetVisitor(visitor, false);
                        ret++;
                    }
                }
                logger.LogInformation($"FixConnectVisitorsWithEmployeeId done {ret}");
                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Global admin method to reset all registrations
        /// </summary>
        /// <returns></returns>
        [HttpPost("DeleteAllRegistrations")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> DeleteAllRegistrations()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"DeleteAllRegistrations by {User.GetEmail()}");
                var ret = await visitorRepository.DropAllRegistrations();
                logger.LogInformation($"DeleteAllRegistrations done {ret}");
                return Ok(ret);
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
        [HttpPost("DeleteOldVisitors")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> DeleteOldVisitors([FromForm] int? days = 14)
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }
                if (!days.HasValue) days = 14;
                logger.LogInformation($"DeleteOldVisitors {User.GetEmail()} {days} {DateTimeOffset.Now.AddDays(-1 * days.Value)}");

                return Ok(await visitorRepository.DeleteOldVisitors(days.Value));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// List all result submissions, if visitor is still in not processed state, add the result submission to the queue
        /// </summary>
        /// <returns></returns>
        [HttpPost("RequeeUnprocessedVisitors")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<int>> RequeeUnprocessedVisitors()
        {
            try
            {
                if (!User.IsAdmin(userRepository))
                {
                    throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);
                }

                logger.LogInformation($"RequeeUnprocessedVisitors");
                var places = (await placeRepository.ListAll()).Where(place => place.PlaceProviderId == User.GetPlaceProvider()).Select(p => p.Id).ToHashSet();
                var results = await visitorRepository.ExportResultSubmissions(places: places);
                var tested = await visitorRepository.ListTestedVisitors(User.GetPlaceProvider(), DateTimeOffset.Now, silent: true);
                var visitors = new Dictionary<string, VisitorTimezoned>();
                foreach (var visitor in tested.OrderBy(t => t.LastUpdate))
                {
                    visitors[visitor.TestingSet] = visitor;
                }
                var ret = 0;
                foreach (var result in results)
                {
                    if (!string.IsNullOrEmpty(result.TestingSetId) && visitors.ContainsKey(result.TestingSetId))
                    {
                        if (visitors[result.TestingSetId].Result == TestResult.TestIsBeingProcessing
                            || !visitors[result.TestingSetId].ResultNotifiedAt.HasValue
                            )
                        {
                            await visitorRepository.AddToResultQueue(result.Id);
                            ret++;
                        }
                    }
                }
                logger.LogInformation($"RequeeUnprocessedVisitors done {ret}");

                return Ok(ret);
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
                    foreach (var visitor in await visitorRepository.ListTestedVisitors(User.GetPlaceProvider()))
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
