// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Persistence
{
    using System;
    using MessagePack;

    [MessagePackObject]
    public sealed class Session
    {
        [SerializationConstructor]
        public Session(
            string userName,
            int sessionId,
            string accountId,
            string[] accessTokens,
            string clientId,
            bool isSupportUser,
            string deviceKey)
        {
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            AccountId = accountId;
            AccessTokens = accessTokens ?? throw new ArgumentNullException(nameof(accessTokens));
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            SessionId = sessionId;
            IsSupportUser = isSupportUser;
            DeviceKey = deviceKey;
        }

        public Session(
            string userName,
            int sessionId,
            string accountId,
            string accessToken,
            string clientId,
            bool isSupportUser,
            string deviceKey)
            : this(userName, sessionId, accountId, new[] { accessToken }, clientId, isSupportUser, deviceKey)
        {
        }

        [Key(0)]
        public string UserName { get; }

        [Key(1)]
        public int SessionId { get; }

        [Key(2)]
        public string AccountId { get; }

        [Key(3)]
        public string[] AccessTokens { get; }

        [Key(4)]
        public string ClientId { get; }

        [Key(5)]
        public bool IsSupportUser { get; }
        
        [Key(6)]
        public string DeviceKey { get; }
    }
}
