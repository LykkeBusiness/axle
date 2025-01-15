﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Axle.Unit.Tests")]
namespace Axle.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using MessagePack;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;

    public class RedisSessionRepository : ISessionRepository
    {
        private readonly IConnectionMultiplexer multiplexer;
        private readonly TimeSpan sessionTimeout;
        private readonly ILogger<RedisSessionRepository> logger;

        public RedisSessionRepository(IConnectionMultiplexer multiplexer, TimeSpan sessionTimeout, ILogger<RedisSessionRepository> logger)
        {
            this.multiplexer = multiplexer;
            this.sessionTimeout = sessionTimeout;
            this.logger = logger;
        }

        private static string ExpirationSetKey => "axle:expirations";

        public async Task Add(Session entity)
        {
            logger.LogDebug($"Trying to add new session: {nameof(entity.AccountId)}:{entity.AccountId}, {nameof(entity.SessionId)}: {entity.SessionId}..");

            var serSession = MessagePackSerializer.Serialize(entity);
            var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var db = multiplexer.GetDatabase();

            var transaction = db.CreateTransaction();

            try
            {
#pragma warning disable 4014
                transaction.StringSetAsync(SessionKey(entity.SessionId), serSession);

                if (entity.IsSupportUser)
                {
                    transaction.StringSetAsync(UserKey(entity.UserName), entity.SessionId);
                }
                else
                {
                    transaction.StringSetAsync(AccountKey(entity.AccountId), entity.SessionId);
                }

                transaction.SortedSetAddAsync(ExpirationSetKey, entity.SessionId, unixNow);
#pragma warning restore 4014
                if (!await transaction.ExecuteAsync())
                {
                    throw new Exception($"Transaction to add new session was not committed.");
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, $"Error occured while creating new session: {nameof(entity.AccountId)}:{entity.AccountId}, {nameof(entity.SessionId)}: {entity.SessionId}.");
            }
        }

        public async Task Update(Session entity)
        {
            await Add(entity);
        }

        public async Task<Session> Get(int sessionId)
        {
            var db = multiplexer.GetDatabase();

            var lastUpdated = await db.SortedSetScoreAsync(ExpirationSetKey, sessionId);

            // No information about session in the expiration set - return null
            if (!lastUpdated.HasValue)
            {
                logger.LogDebug($"{nameof(RedisSessionRepository)}:{nameof(Get)}:{sessionId}: No information about session in the expiration set - return null");
                return null;
            }

            var lastAlive = DateTimeOffset.FromUnixTimeSeconds((long)lastUpdated.Value);
            var utcNow = DateTimeOffset.UtcNow;

            // Session has expired and will be removed on the next expiration check - return null
            if (lastAlive + sessionTimeout < utcNow)
            {
                logger.LogDebug($"{nameof(RedisSessionRepository)}:{nameof(Get)}:{sessionId}: Session has expired and will be removed on the next expiration check - return null");
                return null;
            }

            var serialized = await db.StringGetAsync(SessionKey(sessionId));

            // Edge case - will only happen if the session gets deleted in between fetching its last update time
            // and retrieving the session itself
            if (serialized.IsNull)
            {
                logger.LogDebug($"{nameof(RedisSessionRepository)}:{nameof(Get)}:{sessionId}: Edge case - will only happen if the session gets deleted in between fetching its last update time and retrieving the session itself");
                return null;
            }

            return MessagePackSerializer.Deserialize<Session>(serialized);
        }

        public async Task<Session> GetByUser(string userName)
        {
            return await GetBySessionKey(UserKey(userName));
        }

        public async Task<int?> GetSessionIdByUser(string userName)
        {
            return await GetSessionIdBySessionKey(UserKey(userName));
        }

        public async Task<Session> GetByAccount(string accountId)
        {
            return await GetBySessionKey(AccountKey(accountId));
        }

        public async Task<int?> GetSessionIdByAccount(string accountId)
        {
            return await GetSessionIdBySessionKey(AccountKey(accountId));
        }

        public async Task Remove(int sessionId, string userName, string accountId)
        {
            var db = multiplexer.GetDatabase();
            await db.SortedSetRemoveAsync(ExpirationSetKey, sessionId);
            await db.KeyDeleteAsync(SessionKey(sessionId));

            // Remove the user/account -> session ID key only if it still contains the same session ID
            await RemoveKeyIfEquals(db, UserKey(userName), sessionId);
            await RemoveKeyIfEquals(db, AccountKey(accountId), sessionId);
        }

        public async Task RefreshSessionTimeouts(IEnumerable<int> sessions)
        {
            var db = multiplexer.GetDatabase();

            var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var entriesToUpdate = sessions.Select(session => new SortedSetEntry(session, unixNow)).ToArray();

            await db.SortedSetAddAsync(ExpirationSetKey, entriesToUpdate);
        }

        public async Task<IEnumerable<Session>> GetExpiredSessions()
        {
            var db = multiplexer.GetDatabase();
            var maxTime = (DateTimeOffset.UtcNow - sessionTimeout).ToUnixTimeSeconds();

            var transaction = db.CreateTransaction();

            // Retrieve the IDs to remove and remove them in one transaction - that way other instances of Axle
            // won't be able to produce duplicate TimeOut activities by retrieving the same range before it gets deleted
            var idsToRemoveTask = transaction.SortedSetRangeByScoreAsync(ExpirationSetKey, stop: maxTime);
#pragma warning disable 4014
            transaction.SortedSetRemoveRangeByScoreAsync(ExpirationSetKey, double.NegativeInfinity, maxTime);
#pragma warning restore 4014

            if (!await transaction.ExecuteAsync())
            {
                throw new Exception($"{nameof(RedisSessionRepository)}:{nameof(GetExpiredSessions)} failed to commit transaction.");
            }

            var serializedSessions = await db.StringGetAsync(
                (await idsToRemoveTask).Select(x => (RedisKey) SessionKey((int) x)).ToArray());

            return serializedSessions.Where(x => !x.IsNull).Select(x => MessagePackSerializer.Deserialize<Session>(x));
        }

        internal static string UserKey(string user) => $"axle:users:{user}";

        internal static string AccountKey(string account) => $"axle:accounts:{account}";

        internal static string SessionKey(int session) => $"axle:sessions:{session}";

        private async Task<int?> GetSessionIdBySessionKey(RedisKey sessionKey)
        {
            string sessionId = await multiplexer.GetDatabase().StringGetAsync(sessionKey);

            logger.LogDebug($"{nameof(RedisSessionRepository)}:{nameof(GetSessionIdBySessionKey)}:{sessionKey} returned {(string.IsNullOrEmpty(sessionId) ? "empty string" : sessionId)}");

            return string.IsNullOrEmpty(sessionId) ? (int?)null : int.Parse(sessionId);
        }

        private async Task<Session> GetBySessionKey(RedisKey sessionKey)
        {
            var sessionId = await GetSessionIdBySessionKey(sessionKey);

            return !sessionId.HasValue ? null : await Get(sessionId.Value);
        }

        private async Task RemoveKeyIfEquals(IDatabase db, RedisKey key, RedisValue value)
        {
            var transaction = db.CreateTransaction();

            transaction.AddCondition(Condition.StringEqual(key, value));
            var deleteTask = transaction.KeyDeleteAsync(key);
            await transaction.ExecuteAsync();
        }
    }
}
