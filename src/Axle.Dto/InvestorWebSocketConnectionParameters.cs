using System;
using JetBrains.Annotations;

namespace Axle.Dto
{
    public class InvestorWebSocketConnectionParameters : WebSocketConnectionParameters
    {
        public InvestorWebSocketConnectionParameters(string accountId,
            string accessToken,
            [CanBeNull] string deviceSessionKey,
            bool isConcurrentConnection) : base(accountId, accessToken, deviceSessionKey, isConcurrentConnection)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentNullException(nameof(accountId));
        }

        protected override bool GetIsSupportUser() => false;
    }
}