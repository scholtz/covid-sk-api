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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CovidMassTesting.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> logger;
        private readonly ISlotRepository slotRepository;
        private readonly IPlaceRepository placeRepository;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;
        public AdminController(
            IConfiguration configuration,
            ILogger<AdminController> logger,
            ISlotRepository slotRepository,
            IPlaceRepository placeRepository,
            IUserRepository userRepository
            )
        {
            this.logger = logger;
            this.slotRepository = slotRepository;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
            this.configuration = configuration;
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
                if (!User.IsAdmin()) throw new Exception("Only admin is allowed to manage time");

                var ret = 0;
                foreach (var item in await placeRepository.ListAll())
                {
                    ret += await slotRepository.CheckSlots(DateTimeOffset.Parse(testingDay, CultureInfo.InvariantCulture).Ticks, item.Id, from, until);
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
                if (!User.IsAdmin()) throw new Exception("Only admin is allowed to invite other users");

                return Ok(await userRepository.Add(new Model.User()
                {
                    Email = email,
                    Name = name,
                    Roles = roles.ToList()
                }));
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
        [HttpPost("RemoveUser")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> RemoveUser([FromForm] string email)
        {
            try
            {
                if (!User.IsAdmin()) throw new Exception("Only admin is allowed to remove users");
                if (User.GetEmail() == email) throw new Exception("You cannot remove yourself");

                var mustKeepUsers = configuration.GetSection("AdminUsers").Get<CovidMassTesting.Model.Settings.User[]>();
                if (mustKeepUsers.Any(u => u.Email == email)) throw new Exception("This user is protected by the configuration");

                return Ok(await userRepository.Remove(email));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

    }
}
