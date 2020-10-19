using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRevolver
{
    public class BaseRequest
    {
        [JsonIgnore]
        public UserSession UserSession { get; set; }
        public string Token { get; set; }
    }
}
