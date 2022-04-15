using System;
using JetBrains.Annotations;

namespace Axle.Dto
{
    public abstract class WebSocketConnectionParameters
    {
        private protected WebSocketConnectionParameters(string accountId,
            string accessToken,
            [CanBeNull] string deviceSessionKey,
            bool isConcurrentConnection)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            AccountId = accountId;
            AccessToken = accessToken;
            DeviceSessionKey = deviceSessionKey;
            IsConcurrentConnection = isConcurrentConnection;
        }

        /// <summary>
        /// Account identifier
        /// </summary>
        public string AccountId { get; }

        /// <summary>
        /// Oauth access token
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        /// Unique device session key
        /// </summary>
        [CanBeNull]
        public string DeviceSessionKey { get; }

        /// <summary>
        /// Specific flag passed by the client to designate if web socket connection is concurrent therefore existing
        /// connections would not be closed
        /// </summary>
        public bool IsConcurrentConnection { get; }

        /// <summary>
        /// Indicates if connection supposed to be established for support user account
        /// </summary>
        public bool IsSupportUser => GetIsSupportUser();

        protected abstract bool GetIsSupportUser();
    }
}