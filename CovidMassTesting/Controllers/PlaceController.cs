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
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace CovidMassTesting.Controllers
{
    /// <summary>
    /// Manages places
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlaceController : ControllerBase
    {
        private readonly IStringLocalizer<PlaceController> localizer;
        private readonly ILogger<PlaceController> logger;
        private readonly IPlaceRepository placeRepository;
        private readonly IUserRepository userRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="logger"></param>
        /// <param name="placeRepository"></param>
        /// <param name="userRepository"></param>
        public PlaceController(
            IStringLocalizer<PlaceController> localizer,
            ILogger<PlaceController> logger,
            IPlaceRepository placeRepository,
            IUserRepository userRepository
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
        }
        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
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
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);

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
                    try
                    {
                        oldPlace = await placeRepository.GetPlace(place.Id);
                    }
                    catch (Exception exc)
                    {
                        logger.LogError(exc, localizer[Controllers_PlaceController.Old_place_not_found].Value);
                    }
                    if (oldPlace == null)
                    {
                        logger.LogInformation(localizer[Controllers_PlaceController.Old_place_not_found].Value);
                        update = false;
                    }
                    else
                    {
                        logger.LogInformation($"Changing place: {Newtonsoft.Json.JsonConvert.SerializeObject(oldPlace)}");
                    }
                }

                logger.LogInformation($"InsertUpdate : {place.Id} {update}");

                if (!update)
                {
                    // new place
                    place.Id = Guid.NewGuid().ToString();
                    await placeRepository.SetPlace(place);
                    logger.LogInformation($"Place {place.Name} has been created");
                }
                else
                {
                    // update existing
                    place.Healthy = oldPlace.Healthy;
                    place.Sick = oldPlace.Sick;
                    place.Registrations = oldPlace.Registrations;
                    place = await placeRepository.SetPlace(place);
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
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_PlaceController.Only_admin_is_allowed_to_manage_testing_places].Value);

                if (place is null)
                {
                    throw new ArgumentNullException(nameof(place));
                }

                if (string.IsNullOrEmpty(place.Id) || await placeRepository.GetPlace(place.Id) == null)
                {
                    // new place
                    throw new Exception(localizer[Controllers_PlaceController.Place_not_found].Value);
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
