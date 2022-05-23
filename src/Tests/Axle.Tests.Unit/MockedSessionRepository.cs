using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Axle.Persistence;

namespace Axle.Tests.Unit
{
    public class MockedSessionRepository : InMemoryRepository<int, Session>, ISessionRepository
    {
        public Task Add(Session entity)
        {
            Add(entity.SessionId, entity);
            
            return Task.CompletedTask;
        }

        public Task Update(Session entity)
        {
            Remove(entity.SessionId);
            Add(entity.SessionId, entity);

            return Task.CompletedTask;
        }

        Task<Session> ISessionRepository.Get(int sessionId)
        {
            var session = Get(sessionId);
            return Task.FromResult(session);
        }

        public Task<Session> GetByUser(string userName)
        {
            var kvp = Find(s => s.UserName == userName).FirstOrDefault();

            if (kvp.Equals(default(KeyValuePair<int, Session>)))
                return Task.FromResult((Session)null);

            return Task.FromResult(kvp.Value);
        }

        public async Task<int?> GetSessionIdByUser(string userName)
        {
            return (await GetByUser(userName))?.SessionId;
        }

        public Task<Session> GetByAccount(string accountId)
        {
            var kvp = Find(s => s.AccountId == accountId).FirstOrDefault();
            
            if (kvp.Equals(default(KeyValuePair<int, Session>)))
                return Task.FromResult((Session)null);

            return Task.FromResult(kvp.Value);
        }

        public async Task<int?> GetSessionIdByAccount(string accountId)
        {
            return (await GetByAccount(accountId)).SessionId;
        }

        public Task Remove(int sessionId, string userName, string accountId)
        {
            Remove(sessionId);

            return Task.CompletedTask;
        }

        public Task RefreshSessionTimeouts(IEnumerable<int> sessions)
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Session>> GetExpiredSessions()
        {
            return Task.FromResult(Enumerable.Empty<Session>());
        }
    }
}