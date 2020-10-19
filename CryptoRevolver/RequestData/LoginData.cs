using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRevolver
{
    public class LoginData: BaseRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
