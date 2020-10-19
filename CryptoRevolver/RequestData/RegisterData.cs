using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRevolver
{
    public class RegisterData: BaseRequest
    {
        [JsonProperty(Required = Required.Always)]
        public string Login { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Password { get; set; }
    }
}
