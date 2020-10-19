using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoRevolver
{
    public class Auth : BaseController
    {
        Service Service;
        public Auth(Service service)
        {
            Service = service;
        }

        [HttpMethod("POST")]
        public LoginResponse Login(LoginData param)
        {
            LoginResponse response = new LoginResponse();
            response.Status = ResponseStatus.Success;
            return response;
        }
    }
}
