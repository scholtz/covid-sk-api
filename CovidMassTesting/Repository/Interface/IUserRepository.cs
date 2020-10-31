using CovidMassTesting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Repository.Interface
{
    public interface IUserRepository
    {
        public Task<bool> Add(User place);
        public Task<IEnumerable<User>> ListAll();
        public Task CreateAdminUsersFromConfiguration();
        public Task<string> Preauthenticate(string email);
        public Task<string> Authenticate(string email, string hash, string data);
        public Task<string> ChangePassword(string email, string oldHash, string newHash);
        public Task<bool> Remove(string email);
        public Task<bool> InAnyGroup(string email, string[] role);
    }
}
