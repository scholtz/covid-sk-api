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
        public Task<bool> ChangePassword(string email, string hash);
    }
}
