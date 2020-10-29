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
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Admin can insert new testing location
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
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
                if (place is null)
                {
                    throw new ArgumentNullException(nameof(place));
                }

                Place oldPlace = null;
                var update = true;
                if (string.IsNullOrEmpty(place.Id))
                {
                    update = false;
                }
                else
                {
                    oldPlace = await placeRepository.GetPlace(place.Id);
                    if (oldPlace == null)
                    {
                        logger.LogInformation("Old place not found");
                        update = false;
                    }
                    else
                    {
                        logger.LogInformation("Changing place: " + Newtonsoft.Json.JsonConvert.SerializeObject(oldPlace));
                    }
                }

                logger.LogInformation($"InsertUpdate : {place.Id} {update}");

                if (!update)
                {
                    // new place
                    place.Id = Guid.NewGuid().ToString();
                    await placeRepository.Set(place);
                    logger.LogInformation($"Place {place.Name} has been created");
                }
                else
                {
                    // update existing
                    place.Healthy = oldPlace.Healthy;
                    place.Sick = oldPlace.Sick;
                    place.Registrations = oldPlace.Registrations;
                    place = await placeRepository.Set(place);
                    logger.LogInformation($"Place {place.Name} has been updated");
                }

                return Ok(place);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Admin can delete testing location
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Delete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Place>> Delete(
            [FromBody] Place place
            )
        {
            try
            {
                if (string.IsNullOrEmpty(place.Id) || await placeRepository.GetPlace(place.Id) == null)
                {
                    // new place
                    throw new Exception("Place not found");
                }
                else
                {
                    // update existing

                    await placeRepository.Delete(place);
                    logger.LogInformation($"Place {place.Name} has been deleted");
                }

                return Ok(place);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
