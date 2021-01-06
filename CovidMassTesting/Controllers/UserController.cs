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
    /// Controller that manages user requests
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IStringLocalizer<UserController> localizer;
        private readonly ILogger<PlaceController> logger;
        private readonly IUserRepository userRepository;
        private readonly IPlaceProviderRepository placeProviderRepository;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="localizer"></param>
        /// <param name="logger"></param>
        /// <param name="userRepository"></param>
        /// <param name="placeProviderRepository"></param>
        public UserController(
            IStringLocalizer<UserController> localizer,
            ILogger<PlaceController> logger,
            IUserRepository userRepository,
            IPlaceProviderRepository placeProviderRepository
            )
        {
            this.localizer = localizer;
            this.logger = logger;
            this.userRepository = userRepository;
            this.placeProviderRepository = placeProviderRepository;
        }
        /// <summary>
        /// List all public information of all users
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("List")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, UserPublic>>> List()
        {
            try
            {
                if (!User.IsAdmin(userRepository)) throw new Exception(localizer[Controllers_UserController.Only_user_with_Admin_role_can_list_users].Value);

                return Ok((await userRepository.ListAll()).ToDictionary(p => p.Email, p => p.ToPublic()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }


        /// <summary>
        /// List invitations to place provider for generic users
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListUserInvites")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<Invitation>>> ListUserInvites()
        {
            try
            {
                return Ok(await userRepository.ListInvitationsByEmail(User.GetEmail()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// List invitations to place provider for generic users
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ProcessInvitation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Invitation>> ProcessInvitation([FromForm] string invitationId, [FromForm] bool accepted)
        {
            try
            {
                if (string.IsNullOrEmpty(invitationId))
                {
                    throw new ArgumentException("Please provide invitationId");
                }

                return Ok(await userRepository.ProcessInvitation(invitationId, accepted, User.GetEmail()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// List invitations to place provider for generic users
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("ListPPInvites")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<Invitation>>> ListPPInvites()
        {
            try
            {
                if (!await User.IsPlaceProviderAdmin(userRepository, placeProviderRepository)) throw new Exception(localizer[Resources.Controllers_AdminController.Only_admin_is_allowed_to_invite_other_users].Value);

                return Ok(await userRepository.ListInvitationsByPP(User.GetPlaceProvider()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Place at which person is assigned. All person's registrations will be placed to this location
        /// </summary>
        /// <param name="placeId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetLocation")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Dictionary<string, UserPublic>>> SetLocation([FromForm] string placeId)
        {
            try
            {
                if (string.IsNullOrEmpty(placeId))
                {
                    throw new ArgumentException(localizer[Controllers_UserController.Place_must_not_be_empty].Value);
                }

                if (!User.IsRegistrationManager(userRepository, placeProviderRepository)) throw new Exception(localizer[Controllers_UserController.Only_user_with_Registration_Manager_role_can_select_his_own_place_].Value);

                return Ok(await userRepository.SetLocation(User.GetEmail(), placeId, User.GetPlaceProvider()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Preauthenticate. Cohash is important part of hash. This method returns cohash
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost("Preauthenticate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<AuthData>> Preauthenticate(
            [FromForm] string email
            )
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException(localizer[Controllers_UserController.Email_must_not_be_empty].Value);
                }

                return Ok(await userRepository.Preauthenticate(email));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Returns JWT token
        /// 
        /// First preauthenticate request must be executed. It returns
        /// {CoData : "..", CoHash : ".."}
        /// 
        /// Hash is:
        /// 
        /// Password = Real Password
        /// 99 repeat of 
        ///  Password = SHA256(Password + CoHash)
        ///  
        /// Hash = SHA256(Password + CoData)
        /// </summary>
        /// <param name="email">User email address</param>
        /// <param name="hash">Hash of CoData, CoHash and password</param>
        /// <returns></returns>
        [HttpPost("Authenticate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> Authenticate(
            [FromForm] string email,
            [FromForm] string hash
            )
        {
            try
            {
                return Ok(await userRepository.Authenticate(email, hash));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Set new password
        /// </summary>
        /// <param name="oldHash"></param>
        /// <param name="newHash"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ChangePassword")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> ChangePassword(
            [FromForm] string oldHash,
            [FromForm] string newHash
            )
        {
            try
            {
                if (User.IsPasswordProtected(userRepository)) { throw new Exception(localizer[Controllers_UserController.This_special_user_cannot_change_the_password_].Value); }

                return Ok(await userRepository.ChangePassword(User.GetEmail(), oldHash, newHash, User.GetPlaceProvider()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }

        /// <summary>
        /// Refresh token after something has been changed.. for example after new place provider registration, or after permission granting
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RefreshToken")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> RefreshToken()
        {
            try
            {
                return Ok(await userRepository.SetPlaceProvider(User.GetEmail(), User.GetPlaceProvider()));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
        /// <summary>
        /// Set place provider
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetPlaceProvider")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<string>> SetPlaceProvider(
            [FromForm] string placeProviderId
            )
        {
            try
            {
                if (!await User.IsAuthorizedToLogAsCompany(userRepository, placeProviderRepository, placeProviderId)) { throw new Exception("Nie je možné autorizovať užívateľa za vybranú spoločnosť"); }

                return Ok(await userRepository.SetPlaceProvider(User.GetEmail(), placeProviderId));
            }
            catch (Exception exc)
            {
                logger.LogError(exc, exc.Message);

                return BadRequest(new ProblemDetails() { Detail = exc.Message });
            }
        }
    }
}
