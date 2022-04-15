using System.Collections.Generic;
using System.Linq;

namespace Axle.Persistence
{
    public static class SessionExtensions
    {
        public static Session AddToken(this Session session, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return session;
            
            var newAccessTokens = new HashSet<string>(session.AccessTokens) { accessToken };

            return new Session(session.UserName,
                session.SessionId,
                session.AccountId,
                newAccessTokens.ToArray(),
                session.ClientId,
                session.IsSupportUser,
                session.DeviceKey);
        }

        public static bool SameDevice(this Session session, string deviceKey)
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
                return false;

            if (string.IsNullOrWhiteSpace(session.DeviceKey))
                return false;

            return session.DeviceKey == deviceKey;
        }
    }
}