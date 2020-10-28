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
        [HttpPost("Create")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Place>> Create(
            [FromForm] string address,
            [FromForm] string lat,
            [FromForm] string lng,
            [FromForm] bool isDriveIn,
            [FromForm] bool isWalkIn
            )
        {
            try
            {
                var ret = new Place()
                {
                    Id = Guid.NewGuid().ToString(),
                    IsDriveIn = isDriveIn,
                    IsWalkIn = isWalkIn,
                    Address = address,
                    Lat = decimal.Parse(lat.Replace(",", "."), CultureInfo.InvariantCulture),
                    Lng = decimal.Parse(lng.Replace(",", "."), CultureInfo.InvariantCulture),
                };
                await placeRepository.Add(ret);
                logger.LogInformation($"Place {ret.Name} has been created");
                return Ok(ret);
            }
            catch (Exception exc)
            {
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
