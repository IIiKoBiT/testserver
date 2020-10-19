using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace CryptoRevolver
{
    public class UserSession
    {
        // TODO: Дописать удаление токенов спустя какое то время
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
