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
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> logger;
        private readonly ISlotRepository slotRepository;
        private readonly IPlaceRepository placeRepository;
        public AdminController(
            ILogger<AdminController> logger,
            ISlotRepository slotRepository,
            IPlaceRepository placeRepository
            )
        {
            this.logger = logger;
            this.slotRepository = slotRepository;
            this.placeRepository = placeRepository;
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
                var ret = 0;
                foreach (var item in await placeRepository.ListAll())
                {
                    ret += await slotRepository.CheckSlots(DateTimeOffset.Parse(testingDay, CultureInfo.InvariantCulture).Ticks, item.Id, from, until);
                }

                return Ok(ret);
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message + (exc.InnerException != null ? $";\n{exc.InnerException.Message}" : "") + "\n" + exc.StackTrace, Title = exc.Message, Type = exc.GetType().ToString() });
            }
        }
        [HttpGet("ListHourSlotsByPlaceAndDaySlotId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, UserPublic>>> ListHourSlotsByPlaceAndDaySlotId([FromQuery] string placeId, [FromQuery] long daySlotId)
        {
            try
            {
                return Ok((await slotRepository.ListHourSlotsByPlaceAndDaySlotId(placeId, daySlotId)).ToDictionary(p => p.Time.Ticks, p => p));
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message + (exc.InnerException != null ? $";\n{exc.InnerException.Message}" : "") + "\n" + exc.StackTrace, Title = exc.Message, Type = exc.GetType().ToString() });
            }
        }
        [HttpGet("ListMinuteSlotsByPlaceAndHourSlotId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, UserPublic>>> ListMinuteSlotsByPlaceAndHourSlotId([FromQuery] string placeId, [FromQuery] long hourSlotId)
        {
            try
            {
                return Ok((await slotRepository.ListMinuteSlotsByPlaceAndHourSlotId(placeId, hourSlotId)).ToDictionary(p => p.Time.Ticks, p => p));
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message + (exc.InnerException != null ? $";\n{exc.InnerException.Message}" : "") + "\n" + exc.StackTrace, Title = exc.Message, Type = exc.GetType().ToString() });
            }
        }

    }
}
