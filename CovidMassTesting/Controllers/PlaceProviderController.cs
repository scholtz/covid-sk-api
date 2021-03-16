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
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<PlaceProviderController> localizer;
        private readonly ILogger<PlaceProviderController> logger;
        private readonly IPlaceRepository placeRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        private readonly IUserRepository userRepository;
        private readonly IVisitorRepository visitorRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="localizer"></param>
        /// <param name="logger"></param>
        /// <param name="placeProviderRepository"></param>
        /// <param name="placeRepository"></param>
        /// <param name="userRepository"></param>
        /// <param name="visitorRepository"></param>
        public PlaceProviderController(
            IConfiguration configuration,
            IStringLocalizer<PlaceProviderController> localizer,
            ILogger<PlaceProviderController> logger,
            IPlaceProviderRepository placeProviderRepository,
            IPlaceRepository placeRepository,
            IUserRepository userRepository,
            IVisitorRepository visitorRepository
            )
        {
            this.configuration = configuration;
            this.localizer = localizer;
            this.logger = logger;
            this.placeRepository = placeRepository;
            this.userRepository = userRepository;
            this.placeProviderRepository = placeProviderRepository;
            this.visitorRepository = visitorRepository;
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
                if (!string.IsNullOrEmpty(configuration["MaxPlaceProviders"]))
                {
                    if (int.TryParse(configuration["MaxPlaceProviders"], out var limit))
                    {
                        var list = await placeProviderRepository.ListPublic();
                        if (limit <= list.Count())
                        {
                            throw new Exception("Place provider limit has been reached");
                        }
                    }
                }
                var ret = await placeProviderRepository.Register(testingPlaceProvider);
                if (ret != null)
                {
                    try
                    {
                        await userRepository.Invite(new Invitation()
                        {
                            CompanyName = testingPlaceProvider.CompanyName,
                            Email = testingPlaceProvider.MainEmail,
                            InvitationMessage = "Registrácia správcu odberných miest",
                            InvitationTime = DateTimeOffset.Now,
                            InviterName = "System administrator",
                            Name = testingPlaceProvider.MainContact,
                            Phone = testingPlaceProvider.PrivatePhone,
                            PlaceProviderId = ret.PlaceProviderId,
                            Status = InvitationStatus.Invited,
                        });

                    }
                    catch (Exception exc)
                    {
                        logger.LogInformation(exc.Message);
                        // user exists
                    }
                }
                return Ok(ret);
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
        /// Updates updatable information about place provider
        /// </summary>
        /// <param name="placeProvider"></param>
        /// <returns></returns>
        [HttpPost("UpdatePP")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlaceProvider>> UpdatePP([FromBody] PlaceProvider placeProvider)
        {
            try
            {
                if (placeProvider is null)
                {
                    throw new ArgumentNullException(nameof(placeProvider));
                }

                if (string.IsNullOrEmpty(placeProvider.PlaceProviderId))
                {
                    throw new Exception("Invalid data has been received");
                }
                if (string.IsNullOrEmpty(placeProvider.MainContact))
                {
                    throw new Exception("Place provide your name in the registration form");
                }
                if (string.IsNullOrEmpty(placeProvider.MainEmail) || !placeProvider.MainEmail.IsValidEmail())
                {
                    throw new Exception("Place provide valid main email");
                }
                placeProvider.PrivatePhone = placeProvider.PrivatePhone.FormatPhone();
                if (string.IsNullOrEmpty(placeProvider.PrivatePhone) || !placeProvider.PrivatePhone.IsValidPhoneNumber())
                {
                    throw new Exception("Place provide valid contact phone number in form +421 907 000 000");
                }

                var toUpdate = await placeProviderRepository.GetPlaceProvider(placeProvider.PlaceProviderId);
                if (toUpdate == null) throw new Exception("Place provider has not been found");

                if (!string.IsNullOrEmpty(placeProvider.CompanyId))
                {
                    toUpdate.CompanyId = placeProvider.CompanyId;
                }
                if (!string.IsNullOrEmpty(placeProvider.CompanyName))
                {
                    toUpdate.CompanyName = placeProvider.CompanyName;
                }
                if (!string.IsNullOrEmpty(placeProvider.Country))
                {
                    toUpdate.Country = placeProvider.Country;
                }
                if (!string.IsNullOrEmpty(placeProvider.CSS))
                {
                    toUpdate.CSS = placeProvider.CSS;
                }
                if (!string.IsNullOrEmpty(placeProvider.Logo))
                {
                    toUpdate.Logo = placeProvider.Logo;
                }
                if (!string.IsNullOrEmpty(placeProvider.MainContact))
                {
                    toUpdate.MainContact = placeProvider.MainContact;
                }
                if (!string.IsNullOrEmpty(placeProvider.MainEmail))
                {
                    toUpdate.MainEmail = placeProvider.MainEmail;
                }
                if (!string.IsNullOrEmpty(placeProvider.PrivatePhone))
                {
                    toUpdate.PrivatePhone = placeProvider.PrivatePhone;
                }
                if (!string.IsNullOrEmpty(placeProvider.PublicEmail))
                {
                    toUpdate.PublicEmail = placeProvider.PublicEmail;
                }
                if (!string.IsNullOrEmpty(placeProvider.PublicPhone))
                {
                    toUpdate.PublicPhone = placeProvider.PublicPhone;
                }
                if (!string.IsNullOrEmpty(placeProvider.VAT))
                {
                    toUpdate.VAT = placeProvider.VAT;
                }
                if (!string.IsNullOrEmpty(placeProvider.Web))
                {
                    toUpdate.Web = placeProvider.Web;
                }

                return Ok(await placeProviderRepository.SetPlaceProvider(toUpdate));

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
        /// Updates encrypted data for place provider
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UpdateSensitiveData")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> UpdateSensitiveData([FromBody] PlaceProviderSensitiveData data)
        {
            try
            {
                if (data is null)
                {
                    throw new ArgumentNullException(nameof(data));
                }

                if (string.IsNullOrEmpty(data.PlaceProviderId))
                {
                    throw new Exception("Invalid data has been received");
                }
                if (User.GetPlaceProvider() != data.PlaceProviderId) throw new Exception("Please select place provider");
                return Ok(await placeProviderRepository.SetPlaceProviderSensitiveData(data, false));
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
        /// Updates encrypted data for place provider
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetSensitiveData")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PlaceProviderSensitiveData>> GetSensitiveData()
        {
            try
            {
                if (string.IsNullOrEmpty(User.GetPlaceProvider()))
                {
                    throw new ArgumentNullException("Please select place provider");
                }
                var ret = await placeProviderRepository.GetPlaceProviderSensitiveData(User.GetPlaceProvider());
                if (ret != null)
                {
                    ret.EZdraviePass = "";
                }
                return Ok(ret);
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
                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());


                return Ok(await userRepository.Invite(
                    new Model.Invitation()
                    {
                        CompanyName = pp.CompanyName,
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
                return Ok(await placeProviderRepository.ListPrivate(User.GetEmail(), User.IsAdmin(userRepository)));
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
        /// Allocate 
        /// </summary>
        /// <param name="allocations"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AllocatePersonsToPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PersonAllocation>>> AllocatePersonsToPlace(
            [FromBody] PersonAllocation[] allocations,
            [FromQuery] string placeId
            )
        {

            try
            {
                if (allocations is null || allocations.Length == 0)
                {
                    throw new ArgumentNullException(nameof(allocations));
                }

                if (string.IsNullOrEmpty(placeId))
                {
                    throw new ArgumentNullException(nameof(placeId));
                }


                if (!await User.IsPlaceAdmin(userRepository, placeProviderRepository, placeRepository, placeId)) throw new Exception("Only place provider admin can assign person to place");

                var ret = new List<PersonAllocation>();
                foreach (var allocation in allocations)
                {
                    ret.Add(await placeProviderRepository.AllocatePerson(allocation, placeId));
                }
                return Ok(ret);
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
        /// Allocate 
        /// </summary>
        /// <param name="allocationId"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RemoveAllocationAtPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> RemoveAllocationAtPlace(
            [FromForm] string allocationId,
            [FromForm] string placeId
            )
        {

            try
            {
                if (string.IsNullOrEmpty(allocationId))
                {
                    throw new ArgumentNullException(nameof(allocationId));
                }
                if (string.IsNullOrEmpty(placeId))
                {
                    throw new ArgumentNullException(nameof(placeId));
                }


                if (!await User.IsPlaceAdmin(userRepository, placeProviderRepository, placeRepository, placeId)) throw new Exception("Only place provider admin can assign person to place");

                return Ok(await placeProviderRepository.RemovePersonAllocation(allocationId, placeId));
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
        /// List HR allocations to specific place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListPlaceAllocations")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PersonAllocation>>> ListPlaceAllocations(
            [FromQuery] string placeId
            )
        {

            try
            {
                if (string.IsNullOrEmpty(placeId))
                {
                    throw new ArgumentNullException(nameof(placeId));
                }

                if (!await User.IsPlaceAdmin(userRepository, placeProviderRepository, placeRepository, placeId)) throw new Exception("Only place provider admin can assign person to place");

                return Ok(await placeProviderRepository.ListAllocations(placeId));
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
        /// Administrator is allowed to create product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("CreateProduct")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
        {

            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                product.Id = Guid.NewGuid().ToString();
                product.CreatedOn = DateTimeOffset.Now;
                product.LastUpdate = product.CreatedOn;
                return Ok(await placeProviderRepository.AddProduct(User.GetPlaceProvider(), product));
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
        /// Administrator is allowed to update product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UpdateProduct")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Product>> UpdateProduct([FromBody] Product product)
        {

            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);

                product.LastUpdate = DateTimeOffset.Now;
                return Ok(await placeProviderRepository.SetProduct(User.GetPlaceProvider(), product));
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
        /// Administrator is allowed to delete product or service which he sells or serve at the testing place
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DeleteProduct")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<bool>> DeleteProduct([FromBody] Product product)
        {

            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                return Ok(await placeProviderRepository.DeleteProduct(User.GetPlaceProvider(), product));
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
        /// Administrator is allowed to list pp products
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListProducts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<Product>>> ListProducts()
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);
                return Ok(await placeProviderRepository.ListProducts(User.GetPlaceProvider()));
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
        /// Administrator is allowed to list pp products
        /// </summary>
        /// <returns></returns>

        [HttpGet("GetStats")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<Dictionary<DateTimeOffset, long>>>> GetStats(string statsType, string placeProviderId)
        {
            try
            {
                if (string.IsNullOrEmpty(statsType))
                {
                    throw new ArgumentException($"'{nameof(statsType)}' cannot be null or empty.", nameof(statsType));
                }
                if (string.IsNullOrEmpty(placeProviderId))
                {
                    throw new ArgumentException($"'{nameof(placeProviderId)}' cannot be null or empty.", nameof(placeProviderId));
                }

                return Ok(await visitorRepository.GetPPStats(statsType, placeProviderId));
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
        /// Everyone can list place products at place
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        [HttpGet("ListPlaceProductByPlace")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProductWithPlace>>> ListPlaceProductByPlace(
            [FromQuery] string placeId
            )
        {
            try
            {
                var place = await placeRepository.GetPlace(placeId);
                if (place == null) throw new Exception("Place not found");
                var pp = await placeProviderRepository.GetPlaceProvider(place.PlaceProviderId);
                if (pp == null) throw new Exception("Place provider not found");
                var ret = await placeRepository.ListPlaceProductByPlace(placeId);
                var places2 = IPlaceProviderRepository.ExtendByAllProducts(ret, pp, new string[] { placeId });
                return Ok(places2.Select(ppr => ppr.ToExtendedModel(placeProviderRepository).Result));
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
        /// Everyone can list place products at place provider
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        [HttpGet("ListPlaceProductByPlaceProvider")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProduct>>> ListPlaceProductByPlaceProvider(
            [FromQuery] string placeProviderId
            )
        {
            try
            {
                var pp = await placeProviderRepository.GetPlaceProvider(placeProviderId);
                if (pp == null) throw new Exception("Place provider not found");
                var ret = await placeRepository.ListPlaceProductByPlaceProvider(pp);
                var places = await placeRepository.ListAll();

                return Ok(IPlaceProviderRepository.ExtendByAllProducts(ret, pp, places.Where(pp => pp.PlaceProviderId == placeProviderId).Select(p => p.Id).ToArray()));
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
        /// Everyone can list place products at place provider
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListPlaceProduct")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProductWithPlace>>> ListPlaceProduct()
        {
            try
            {
                var pp = await placeProviderRepository.GetPlaceProvider(User.GetPlaceProvider());
                if (pp == null) throw new Exception("Place provider not found");
                var ret = await placeRepository.ListPlaceProductByPlaceProvider(pp);
                var places = await placeRepository.ListAll();
                var places2 = IPlaceProviderRepository.ExtendByAllProducts(ret, pp, places.Where(pp => pp.PlaceProviderId == User.GetPlaceProvider()).Select(p => p.Id).ToArray());

                return Ok(places2.Select(ppr => ppr.ToExtendedModel(placeProviderRepository).Result));
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
        /// Everyone can list place products at place provider
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        [HttpGet("ListPlaceProductByCategory")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<PlaceProduct>>> ListPlaceProductByCategory(
            [FromQuery] string category
            )
        {
            try
            {
                return Ok(await placeProviderRepository.ListPlaceProductByCategory(category));
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
