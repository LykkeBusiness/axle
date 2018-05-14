﻿namespace Axle.Persistence
{
    using System.Collections.Generic;
    using System.Linq;

    // NOTE (Marta): This is going to be replaced in the future with a repository that persists data.
    public sealed class InMemorySessionRepository : ISessionRepository
    {
        private readonly IDictionary<string, SessionState> sessions = new Dictionary<string, SessionState>();

        public void Add(string sessionId, SessionState sessionState)
        {
            this.sessions.Add(sessionId, sessionState);
        }

        public SessionState Get(string sessionId)
        {
            return this.sessions[sessionId];
        }

        public bool TryGet(string sessionId, out SessionState userId)
        {
            return this.sessions.TryGetValue(sessionId, out userId);
        }

        public void Remove(string sessionId)
        {
            this.sessions.Remove(sessionId);
        }

        public IEnumerable<SessionState> GetByUser(string userId)
        {
            return this.sessions.Where(x => x.Value.UserId == userId).Select(kv => kv.Value).ToList();
        }
    }
}
