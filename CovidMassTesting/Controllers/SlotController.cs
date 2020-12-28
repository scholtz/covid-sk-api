using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Model;
using CovidMassTesting.Repository;
using CovidMassTesting.Repository.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

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
        /// <returns></returns>
        [HttpGet("ListDaySlotsByPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Slot1Day>>> ListDaySlotsByPlace([FromQuery] string placeId)
        {
            try
            {
                return Ok((
                    await slotRepository.ListDaySlotsByPlace(placeId))
                        .Where(s => s != null)
                        .OrderBy(s => s.SlotId)
                        .ToDictionary(p => p.Time.Ticks, p => p));
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
        /// <returns></returns>
        [HttpGet("ListHourSlotsByPlaceAndDaySlotId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Slot1Hour>>> ListHourSlotsByPlaceAndDaySlotId([FromQuery] string placeId, [FromQuery] long daySlotId)
        {
            try
            {
                return Ok((await slotRepository.ListHourSlotsByPlaceAndDaySlotId(placeId, daySlotId)).OrderBy(s => s.SlotId).ToDictionary(p => p.Time.Ticks, p => p));
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
        /// <returns></returns>
        [HttpGet("ListMinuteSlotsByPlaceAndHourSlotId")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Slot5Min>>> ListMinuteSlotsByPlaceAndHourSlotId([FromQuery] string placeId, [FromQuery] long hourSlotId)
        {
            try
            {
                return Ok((await slotRepository.ListMinuteSlotsByPlaceAndHourSlotId(placeId, hourSlotId)).OrderBy(s => s.SlotId).ToDictionary(p => p.Time.Ticks, p => p));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

    }
}
