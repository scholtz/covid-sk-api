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
using Microsoft.Extensions.Logging;

namespace CovidMassTesting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlaceController : ControllerBase
    {
        private readonly ILogger<PlaceController> logger;
        private readonly IPlaceRepository placeRepository;
        public PlaceController(
            ILogger<PlaceController> logger,
            IPlaceRepository placeRepository
            )
        {
            this.logger = logger;
            this.placeRepository = placeRepository;
        }

        [HttpGet("List")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, Place>>> List()
        {
            try
            {
                return Ok((await placeRepository.ListAll()).ToDictionary(p => p.Id, p => p));
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        [Authorize]
        [HttpPost("InsertOrUpdate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Place>> InsertOrUpdate(
            [FromBody] Place place
            )
        {
            try
            {
                if (string.IsNullOrEmpty(place.Id) || await placeRepository.GetPlace(place.Id) == null)
                {
                    // new place
                    place.Id = Guid.NewGuid().ToString();
                    await placeRepository.Set(place);
                    logger.LogInformation($"Place {place.Name} has been created");
                }
                else
                {
                    // update existing

                    await placeRepository.Set(place);
                    logger.LogInformation($"Place {place.Name} has been updated");
                }

                return Ok(place);
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
