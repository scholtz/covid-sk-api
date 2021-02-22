using CovidMassTesting.Model;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// Slot controller manages time slots at the sampling places
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SlotController : ControllerBase
    {
        private readonly ILogger<SlotController> logger;
        private readonly ISlotRepository slotRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="slotRepository"></param>
        public SlotController(
            ILogger<SlotController> logger,
            ISlotRepository slotRepository
            )
        {
            this.logger = logger;
            this.slotRepository = slotRepository;
        }
        /// <summary>
        /// Shows available days per place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="showAll">0|1 If not filled in or 0, it does not show past slots</param>
        /// <returns></returns>
        [HttpGet("ListDaySlotsByPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Slot1Day>>> ListDaySlotsByPlace([FromQuery] string placeId, [FromQuery] string showAll = "0")
        {
            try
            {
                var slots = (await slotRepository.ListDaySlotsByPlace(placeId)).Where(s => s != null);
                if (showAll != "1")
                {
                    slots = slots.Where(s => s.Time >= DateTimeOffset.UtcNow.AddDays(-1));
                }
                return Ok(slots.OrderBy(s => s.SlotId).ToDictionary(p => p.Time.Ticks, p => p));
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
        /// Shows available hours at specific day and place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="daySlotId"></param>
        /// <param name="showAll">0|1 If not filled in or 0, it does not show past slots</param>
        /// <returns></returns>
        [HttpGet("ListHourSlotsByPlaceAndDaySlotId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Slot1Hour>>> ListHourSlotsByPlaceAndDaySlotId(
            [FromQuery] string placeId,
            [FromQuery] long daySlotId,
            [FromQuery] string showAll = "0"
            )
        {
            try
            {
                var slots = await slotRepository.ListHourSlotsByPlaceAndDaySlotId(placeId, daySlotId);
                if (showAll != "1")
                {
                    slots = slots.Where(s => s.Time >= DateTimeOffset.UtcNow.AddHours(-1));
                }
                return Ok(slots.OrderBy(s => s.SlotId).ToDictionary(p => p.Time.Ticks, p => p));
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
        /// List specific minute slots at specific hour and place
        /// </summary>
        /// <param name="placeId"></param>
        /// <param name="hourSlotId"></param>
        /// <param name="showAll">0|1 If not filled in or 0, it does not show past slots</param>
        /// <returns></returns>
        [HttpGet("ListMinuteSlotsByPlaceAndHourSlotId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Slot5Min>>> ListMinuteSlotsByPlaceAndHourSlotId(
            [FromQuery] string placeId,
            [FromQuery] long hourSlotId,
            [FromQuery] string showAll = "0")
        {
            try
            {
                var slots = await slotRepository.ListMinuteSlotsByPlaceAndHourSlotId(placeId, hourSlotId);
                if (showAll != "1")
                {
                    slots = slots.Where(s =>
                    {
                        //logger.LogInformation($"DEBUGTIME: {s.Time.ToString("R")} {s.TimeFromTicks.ToString("R")} {DateTimeOffset.Now.AddMinutes(-5)} {DateTimeOffset.Compare(s.Time, DateTimeOffset.UtcNow.AddMinutes(-5))}");
                        return s.TimeFromTicks >= DateTimeOffset.UtcNow.AddMinutes(-5);
                    });
                }
                return Ok(slots.OrderBy(s => s.SlotId).ToDictionary(p => p.Time.Ticks, p => p));
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
