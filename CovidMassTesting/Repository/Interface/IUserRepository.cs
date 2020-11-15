using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="place"></param>
        /// <returns></returns>
        public Task<bool> Add(User user);
        /// <summary>
        /// List all users
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<User>> ListAll();
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
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<string> Authenticate(string email, string hash);
        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="email"></param>
        /// <param name="oldHash"></param>
        /// <param name="newHash"></param>
        /// <returns></returns>
        public Task<string> ChangePassword(string email, string oldHash, string newHash);
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
        /// <returns></returns>
        public Task<bool> InAnyGroup(string email, string[] role);
        /// <summary>
        /// Registration Manager can select place. All his registrations will be placed at this location
        /// </summary>
        /// <param name="v"></param>
        /// <param name="placeId"></param>
        /// <returns></returns>
        public Task<bool> SetLocation(string v, string placeId);
        /// <summary>
        /// Get public user data.. eg location
        /// </summary>
        /// <param name="managerEmail"></param>
        /// <returns></returns>
        public Task<UserPublic> GetPublicUser(string managerEmail);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Task<bool> DropDatabaseAuthorize(string email, string hash);
        /// <summary>
        /// Administrator has power to delete everything in the database. Password confirmation is required.
        /// </summary>
        /// <returns></returns>
        public Task<int> DropAllData();
    }
}
