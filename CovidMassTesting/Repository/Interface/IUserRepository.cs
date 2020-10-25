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

    }
}
