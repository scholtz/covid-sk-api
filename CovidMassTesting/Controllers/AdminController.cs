//#define UseFixes
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using CovidMassTesting.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<AdminController> logger;
        private readonly ISlotRepository slotRepository;
        private readonly IPlaceRepository placeRepository;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;
        private readonly IVisitorRepository visitorRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="slotRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="placeProviderRepository"></param>
        public AdminController(
            IStringLocalizer<AdminController> localizer,
            IConfiguration configuration,
            ILogger<AdminController> logger,
            ISlotRepository slotRepository,
            IPlaceRepository placeRepository,
            IUserRepository userRepository,
            IVisitorRepository visitorRepository,
            IPlaceProviderRepository placeProviderRepository
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.slotRepository = slotRepository;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
            this.configuration = configuration;
            this.visitorRepository = visitorRepository;
            this.placeProviderRepository = placeProviderRepository;
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
                    throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_manage_time].Value);

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
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);

                return Ok(await userRepository.Add(new Model.User()
                {
                    Email = email,
                    Name = name,
                    Roles = roles.ToList()
                }, User.GetName(), ""));
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

                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);


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
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_remove_users].Value);
                if (User.GetEmail() == email) throw new Exception(localizer[Controllers_AdminController.You_cannot_remove_yourself].Value);

                var mustKeepUsers = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
                if (mustKeepUsers.Any(u => u.Email == email)) throw new Exception(localizer[Controllers_AdminController.This_user_is_protected_by_the_configuration].Value);

                return Ok(await userRepository.Remove(email));
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
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_AdminController.Only_admin_is_allowed_to_drop_database].Value);
                var drop = await userRepository.DropDatabaseAuthorize(User.GetEmail(), hash);
                if (!drop) throw new Exception(localizer[Controllers_AdminController.Invalid_user_or_password].Value);

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
