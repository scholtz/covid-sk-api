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
    /// <summary>
    /// This controller manages public registrations
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class VisitorController : ControllerBase
    {
        private readonly ILogger<VisitorController> logger;
        private readonly IVisitorRepository visitorRepository;
        private readonly ISlotRepository slotRepository;
        private readonly IPlaceRepository placeRepository;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="slotRepository"></param>
        /// <param name="visitorRepository"></param>
        /// <param name="placeRepository"></param>
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
                    visitor.Phone = "";
                }

                var place = await placeRepository.GetPlace(visitor.ChosenPlaceId);
                if (place == null) { throw new Exception("We are not able to find chosen testing place"); }
                var slotM = await slotRepository.Get5MinSlot(visitor.ChosenPlaceId, visitor.ChosenSlot);
                if (slotM == null) { throw new Exception("We are not able to find chosen slot"); }
                var slotH = await slotRepository.GetHourSlot(visitor.ChosenPlaceId, slotM.HourSlotId);
                if (slotH == null) { throw new Exception("We are not able to find chosen hour slot"); }
                var slotD = await slotRepository.GetDaySlot(visitor.ChosenPlaceId, slotH.DaySlotId);

                if (slotM.Registrations >= place.LimitPer5MinSlot)
                {
                    throw new Exception("Tento 5-minútový časový slot má kapacitu zaplnenú.");
                }
                if (slotH.Registrations >= place.LimitPer1HourSlot)
                {
                    throw new Exception("Tento hodinový časový slot má kapacitu zaplnenú.");
                }
                Visitor previous = null;
                switch (visitor.PersonType)
                {
                    case "idcard":
                    case "child":
                        if (!string.IsNullOrEmpty(visitor.RC))
                        {
                            previous = await visitorRepository.GetVisitorByPersonalNumber(visitor.RC);
                        }
                        break;
                    case "foreign":
                        if (!string.IsNullOrEmpty(visitor.Passport))
                        {
                            previous = await visitorRepository.GetVisitorByPersonalNumber(visitor.Passport);
                        }
                        break;
                }
                if (previous == null)
                {
                    // new registration

                    var ret = await visitorRepository.Add(visitor);

                    await slotRepository.IncrementRegistration5MinSlot(slotM);
                    await slotRepository.IncrementRegistrationHourSlot(slotH);
                    await slotRepository.IncrementRegistrationDaySlot(slotD);
                    await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);

                    return Ok(ret);
                }
                else
                {
                    // update registration
                    visitor.Id = previous.Id; // bar code does not change on new registration with the same personal number
                    var ret = await visitorRepository.Set(visitor, false);
                    if (previous.ChosenPlaceId != visitor.ChosenPlaceId)
                    {
                        await placeRepository.DecrementPlaceRegistrations(previous.ChosenPlaceId);
                        await placeRepository.IncrementPlaceRegistrations(visitor.ChosenPlaceId);
                    }
                    if (previous.ChosenSlot != visitor.ChosenSlot)
                    {
                        try
                        {

                            var slotMPrev = await slotRepository.Get5MinSlot(previous.ChosenPlaceId, previous.ChosenSlot);
                            var slotHPrev = await slotRepository.GetHourSlot(previous.ChosenPlaceId, slotMPrev.HourSlotId);
                            var slotDPrev = await slotRepository.GetDaySlot(previous.ChosenPlaceId, slotHPrev.DaySlotId);

                            await slotRepository.DecrementRegistration5MinSlot(slotM);
                            await slotRepository.DecrementRegistrationHourSlot(slotH);
                            await slotRepository.DecrementRegistrationDaySlot(slotD);
                        }
                        catch (Exception exc)
                        {
                            logger.LogError(exc, exc.Message);
                        }
                        await slotRepository.IncrementRegistration5MinSlot(slotM);
                        await slotRepository.IncrementRegistrationHourSlot(slotH);
                        await slotRepository.IncrementRegistrationDaySlot(slotD);
                    }

                    return Ok(ret);
                }
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

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
