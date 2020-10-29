using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
        /// <summary>
        /// Public method for pre registration. Result is returned with Visitor.id which is the main identifier of the visit and should be shown in the bar code
        /// 
        /// Request size is limitted.
        /// </summary>
        /// <param name="visitor"></param>
        /// <returns></returns>
        [HttpPost("Register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [RequestSizeLimit(2000)]
        public async Task<ActionResult<Visitor>> Register([FromBody] Visitor visitor)
        {
            try
            {
                if (visitor is null)
                {
                    throw new ArgumentNullException(nameof(visitor));
                }

                try
                {
                    var addr = new System.Net.Mail.MailAddress(visitor.Email);
                    visitor.Email = addr.Address;
                }
                catch
                {
                    visitor.Email = "";
                }

                visitor.Phone = FormatPhone(visitor.Phone);
                if (!IsPhoneNumber(visitor.Phone))
                {
                    visitor.Phone = visitor.Phone;
                }

                var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                if (place == null) { throw new Exception("We are not able to find chosen testing place"); }
                var slotM = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
                if (slotM == null) { throw new Exception("We are not able to find chosen slot"); }
                var slotH = await slotRepository.GetHourSlot(visitor.ChosenPlaceId, slotM.HourSlotId);
                var slotD = await slotRepository.GetDaySlot(visitor.ChosenPlaceId, slotH.DaySlotId);

                if (slotM.Registrations >= place.LimitPer5MinSlot)
                {
                    throw new Exception("Tento časový slot má kapacitu zaplnenú.");
                }

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
        /// <summary>
        /// Format phone to slovak standard
        /// 
        /// 0800 123 456 convers to +421800123456
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string FormatPhone(string number)
        {
            if (number == null) number = "";
            number = number.Replace(" ", "");
            number = number.Replace("\t", "");
            if (number.StartsWith("00")) number = "+" + number.Substring(2);
            if (number.StartsWith("0")) number = "+421" + number.Substring(1);
            return number;
        }
        /// <summary>
        /// Validates the phone number +421800123456
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{12})$").Success;
        }
    }
}
