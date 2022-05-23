using JetBrains.Annotations;

namespace Axle.Dto
{
    public class SupportWebSocketConnectionParameters : WebSocketConnectionParameters
    {
        public SupportWebSocketConnectionParameters(string accountId,
            string accessToken,
            [CanBeNull] string deviceSessionKey,
            bool isConcurrentConnection) : base(accountId, accessToken, deviceSessionKey, isConcurrentConnection)
        {
        }

        protected override bool GetIsSupportUser() => true;
    }
}