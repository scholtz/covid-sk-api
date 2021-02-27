using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidMassTesting.Model.EZdravie
{
    public class LoginResponse : BaseResponse
    {
        public LoginPayload Payload { get; set; }
    }
}
