using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRevolver
{
    public class Service
    {
        ConcurrentDictionary<string, UserSession> UsersSesions = new ConcurrentDictionary<string, UserSession>();

        public bool CreateSession(string token, int userId)
        {
            UserSession userSession;
            userSession = new UserSession();
            userSession.UserId = userId;
            userSession.CreateDate = DateTime.Now;
            userSession.Token = token;

            return UsersSesions.TryAdd(token, userSession);
        }

        public UserSession CheckSession(string token)
        {
            UserSession userSession;
            UsersSesions.TryGetValue(token, out userSession);

            return userSession;
        }
    }
}
