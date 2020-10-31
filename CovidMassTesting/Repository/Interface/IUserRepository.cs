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
        public Task<string> Preauthenticate(string email);
        /// <summary>
        /// Authenticates user
        /// Returns jwt if successful
        /// </summary>
        /// <param name="email"></param>
        /// <param name="hash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task<string> Authenticate(string email, string hash, string data);
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
    }
}
