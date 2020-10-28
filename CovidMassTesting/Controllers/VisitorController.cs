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
    public class VisitorController : ControllerBase
    {
        private readonly ILogger<VisitorController> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly ISlotRepository slotRepository;
        private readonly IPlaceRepository placeRepository;
        public VisitorController(
            ILogger<VisitorController> logger,
            ISlotRepository slotRepository,
            IVisitorRepository visitorRepository,
            IPlaceRepository placeRepository
            )
        {
            this.logger = logger;
            this.visitorRepository = visitorRepository;
            this.slotRepository = slotRepository;
            this.placeRepository = placeRepository;
        }

        [HttpPost("Register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Visitor>> Register([FromBody] Visitor visitor)
        {
            try
            {
                if (visitor is null)
                {
                    throw new ArgumentNullException(nameof(visitor));
                }
                var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                if (place == null) { throw new Exception("We are not able to find chosen testing place"); }
                var slotM = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
                if (slotM == null) { throw new Exception("We are not able to find chosen slot"); }
                var slotH = await slotRepository.GetHourSlot(visitor.ChosenPlaceId, slotM.HourSlotId);
                var slotD = await slotRepository.GetDaySlot(visitor.ChosenPlaceId, slotH.DaySlotId);

                var ret = await visitorRepository.Add(visitor);

                await slotRepository.IncrementRegistration5MinSlot(slotM);
                await slotRepository.IncrementRegistrationHourSlot(slotH);
                await slotRepository.IncrementRegistrationDaySlot(slotD);
                await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);

                return Ok(ret);
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

    }
}
