using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRevolver
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HttpMethodAttribute : System.Attribute
    {
        public string HttpMethod { get; set; }
        public HttpMethodAttribute() { }

        public HttpMethodAttribute(string httpMethod) 
        {
            HttpMethod = httpMethod;
        }
    }
}
