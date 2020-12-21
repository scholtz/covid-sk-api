using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CovidMassTesting.Helpers;
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
    /// Manages place provider companies
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PlaceProviderController : ControllerBase
    {
        private readonly IStringLocalizer<PlaceProviderController> localizer;
        private readonly ILogger<PlaceProviderController> logger;
        private readonly IPlaceRepository placeRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly IUserRepository userRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="logger"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="userRepository"></param>
        public PlaceProviderController(
            IStringLocalizer<PlaceProviderController> localizer,
            ILogger<PlaceProviderController> logger,
            IPlaceProviderRepository placeProviderRepository,
            IPlaceRepository placeRepository,
            IUserRepository userRepository
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
            this.placeProviderRepository = placeProviderRepository;
        }
        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
        [HttpPost("Register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlaceProvider>> Register([FromBody] PlaceProvider testingPlaceProvider)
        {
            try
            {
                if (testingPlaceProvider is null)
                {
                    throw new ArgumentNullException(nameof(testingPlaceProvider));
                }

                if (string.IsNullOrEmpty(testingPlaceProvider.MainContact))
                {
                    throw new Exception("Place provide your name in the registration form");
                }
                if (string.IsNullOrEmpty(testingPlaceProvider.MainEmail) || !testingPlaceProvider.MainEmail.IsValidEmail())
                {
                    throw new Exception("Place provide valid main email");
                }
                testingPlaceProvider.PrivatePhone = testingPlaceProvider.PrivatePhone.FormatPhone();
                if (string.IsNullOrEmpty(testingPlaceProvider.PrivatePhone) || !testingPlaceProvider.PrivatePhone.IsValidPhoneNumber())
                {
                    throw new Exception("Place provide valid contact phone number in form +421 907 000 000");
                }

                var ret = await placeProviderRepository.Register(testingPlaceProvider);
                if (ret != null)
                {
                    try
                    {
                        await userRepository.Add(new Model.User()
                        {
                            Email = ret.MainEmail,
                            Phone = ret.PrivatePhone,
                            Name = ret.MainContact
                        }, "System administrator", testingPlaceProvider.CompanyName);
                    }
                    catch (Exception exc)
                    {
                        logger.LogInformation(exc.Message);
                        // user exists
                    }
                }
                return Ok(ret);
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }



        /// <summary>
        /// Administrator is allowed to invite other users and set their groups
        /// </summary>
        /// <param name="email"></param>
        /// <param name="name"></param>
        /// <param name="phone"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("InviteUserToPP")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> InviteUserToPP([FromForm] string email, [FromForm] string name, [FromForm] string phone, [FromForm] string message)
        {

            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);

                if (!email.IsValidEmail())
                {
                    throw new Exception("Email is in invalid format");
                }
                var phoneFormatted = phone.FormatPhone();
                if (!phoneFormatted.IsValidPhoneNumber())
                {
                    throw new Exception("Phone number seems to be invalid");
                }
                var addr = new System.Net.Mail.MailAddress(email);


                return Ok(await userRepository.Invite(
                    new Model.Invitation()
                    {
                        Email = addr.Address,
                        Name = name,
                        InvitationMessage = message,
                        InviterName = User.GetName(),
                        InvitationTime = DateTimeOffset.Now,
                        Phone = phoneFormatted,
                        PlaceProviderId = User.GetPlaceProvider(),
                        Status = InvitationStatus.Invited,
                    }));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
        [HttpGet("ListPublic")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProvider>>> ListPublic()
        {
            try
            {
                return Ok(await placeProviderRepository.ListPublic());
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// List places
        /// 
        /// Contains live statistics of users, registered, infected and healthy visitors
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListPrivate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProvider>>> ListPrivate()
        {
            try
            {
                return Ok(await placeProviderRepository.ListPrivate(User.GetEmail()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Public method to calculate costs
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="country"></param>
        /// <param name="includeVAT"></param>
        /// <param name="includeSLA"></param>
        /// <param name="includeRegistrations"></param>
        /// <param name="slaFrom"></param>
        /// <param name="slaUntil"></param>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <returns></returns>
        [HttpGet("GetPrice")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProvider>>> GetPrice(
            [FromQuery] string slaLevel,
            [FromQuery] DateTimeOffset slaFrom,
            [FromQuery] DateTimeOffset slaUntil,
            [FromQuery] int registrations,
            [FromQuery] string currency,
            [FromQuery] string country = "",
            [FromQuery] bool includeVAT = false,
            [FromQuery] bool includeSLA = true,
            [FromQuery] bool includeRegistrations = true
            )
        {
            try
            {
                if (!slaLevel.ValidateSLA()) throw new Exception("Invalid SLA Level");
                if (!currency.ValidateCurrency()) throw new Exception("Invalid currency");
                if (registrations < 0) throw new Exception("Invalid registrations");

                if (includeSLA && includeRegistrations)
                {
                    var price = placeProviderRepository.GetPriceWithoutVAT(slaLevel, registrations, currency, slaFrom, slaUntil);
                    if (includeVAT)
                    {
                        var multiplier = placeProviderRepository.GetVATMultiplier(country);
                        price = decimal.Round(price * multiplier, 2);
                    }
                    return Ok(price);
                }
                if (includeSLA)
                {
                    var price = placeProviderRepository.GetPriceWithoutVATSLA(slaLevel, currency, slaFrom, slaUntil);
                    if (includeVAT)
                    {
                        var multiplier = placeProviderRepository.GetVATMultiplier(country);
                        price = decimal.Round(price * multiplier, 2);
                    }
                    return Ok(price);
                }
                if (includeRegistrations)
                {
                    var price = placeProviderRepository.GetPriceWithoutVATRegistrations(registrations, currency);
                    if (includeVAT)
                    {
                        var multiplier = placeProviderRepository.GetVATMultiplier(country);
                        price = decimal.Round(price * multiplier, 2);
                    }
                    return Ok(price);
                }
                throw new Exception("Unknown input");
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Administrator or accountant can issue proforma invoice. This is not real invoice but the document to be able to process the payment in the companies where legislation process does not allow prepayments.
        /// 
        /// We issue real invoices only after the payment is processed.
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("OrderRegistrations")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProvider>>> OrderRegistrations(
            [FromForm] string placeProviderId,
            [FromForm] int registrations,
            [FromForm] string currency
            )
        {
            try
            {
                if (!currency.ValidateCurrency()) throw new Exception("Invalid currency");
                if (registrations < 0) throw new Exception("Invalid registrations");
                if (!User.IsAuthorizedToIssueInvoice(userRepository, placeProviderRepository, placeProviderId)) throw new Exception("You are not authorized to issue invoices for this company. Please contact administrator or accountant.");
                return Ok(placeProviderRepository.IssueProformaInvoiceRegistrations(placeProviderId, registrations, currency));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Administrator or accountant can issue proforma invoice. This is not real invoice but the document to be able to process the payment in the companies where legislation process does not allow prepayments.
        /// 
        /// We issue real invoices only after the payment is processed.
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <param name="slaLevel"></param>
        /// <param name="registrations"></param>
        /// <param name="currency"></param>
        /// <param name="slaFrom"></param>
        /// <param name="slaUntil"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("OrderSLA")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProvider>>> IssueProformaInvoice(
            [FromForm] string placeProviderId,
            [FromForm] string slaLevel,
            [FromForm] DateTimeOffset slaFrom,
            [FromForm] DateTimeOffset slaUntil,
            [FromForm] int registrations,
            [FromForm] string currency
            )
        {
            try
            {
                if (!slaLevel.ValidateSLA()) throw new Exception("Invalid SLA Level");
                if (!currency.ValidateCurrency()) throw new Exception("Invalid currency");
                if (registrations < 0) throw new Exception("Invalid registrations");
                if (!User.IsAuthorizedToIssueInvoice(userRepository, placeProviderRepository, placeProviderId)) throw new Exception("You are not authorized to issue invoices for this company. Please contact administrator or accountant.");
                return Ok(placeProviderRepository.IssueProformaInvoice(placeProviderId, slaLevel, registrations, currency, slaFrom, slaUntil));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);
                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
