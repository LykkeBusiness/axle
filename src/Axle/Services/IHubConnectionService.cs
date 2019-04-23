﻿// (c) Lykke Corporation 2019 - All rights reserved. No copying, adaptation, decompiling, distribution or any other form of use permitted.

namespace Axle.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Axle.Contracts;
    using Microsoft.AspNetCore.SignalR;

    public interface IHubConnectionService
    {
        Task OpenConnection(
            HubCallerContext context,
            string userName,
            string accountId,
            string clientId,
            string accessToken,
            bool isSupportUser);

        void CloseConnection(string connectionId);

        bool TryGetSessionId(string connectionId, out int sessionId);

        IEnumerable<int> GetAllConnectedSessions();

        IEnumerable<string> FindBySessionId(int sessionId);

        Task TerminateConnections(SessionActivityType reason, params string[] connectionIds);
    }
}
