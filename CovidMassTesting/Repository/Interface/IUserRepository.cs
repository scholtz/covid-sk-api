using CovidMassTesting.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    /// <summary>
    /// User repository interface for dependency injection
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Create user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="inviterName"></param>
        /// <param name="companyName"></param>
        /// <returns></returns>
        public Task<bool> Add(User user, string inviterName, string companyName);
        /// <summary>
        /// List all users
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<User>> ListAll(string placeProviderId);
        /// <summary>
        /// Create admins from configuration
        /// </summary>
        /// <returns></returns>
        public Task CreateAdminUsersFromConfiguration();
        /// <summary>
        /// Returns cohash
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<AuthData> Preauthenticate(string email);
        /// <summary>
        /// Authenticates user
        /// Returns jwt if successful
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<string> Authenticate(string email, string hash);
        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="email"></param>
        /// <param name="oldHash"></param>
        /// <param name="newHash"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<string> ChangePassword(string email, string oldHash, string newHash, string placeProviderId);
        /// <summary>
        /// Deletes user
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<bool> Remove(string email);
        /// <summary>
        /// Checks if user is in any of selected groups
        /// </summary>
        /// <param name="email"></param>
        /// <param name="role"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<bool> InAnyGroup(string email, string[] role, string placeProviderId);
        /// <summary>
        /// Registration Manager can select place. All his registrations will be placed at this location
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeId"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<bool> SetLocation(string email, string placeId, string placeProviderId);
        /// <summary>
        /// Get public user data.. eg location
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<UserPublic> GetPublicUser(string email);
        /// <summary>
        /// Issue token with new place provider
        /// </summary>
        /// <param name="email"></param>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<string> SetPlaceProvider(string email, string placeProviderId);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<bool> DropDatabaseAuthorize(string email, string hash);
        /// <summary>
        /// Accept or deny invitation
        /// </summary>
        /// <param name="invitationId"></param>
        /// <param name="accepted"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        public Task<Invitation> ProcessInvitation(string invitationId, bool accepted, string userEmail);

        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
        /// <summary>
        /// Invite person to the place provider company
        /// </summary>
        /// <param name="invitation"></param>
        /// <returns></returns>
        public Task<Invitation> Invite(Invitation invitation);
        /// <summary>
        /// ListInvitationsByEmail
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<IEnumerable<Invitation>> ListInvitationsByEmail(string email);
        /// <summary>
        /// ListInvitationsByEmail
        /// </summary>
        /// <param name="placeProviderId"></param>
        /// <returns></returns>
        public Task<IEnumerable<Invitation>> ListInvitationsByPP(string placeProviderId);
    }
}
