﻿// (c) Lykke Corporation 2019 - All rights reserved. No copying, adaptation, decompiling, distribution or any other form of use permitted.

namespace Axle.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Axle.Contracts;
    using Axle.Dto;
    using Axle.Extensions;
    using Axle.Hubs;
    using Axle.Persistence;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Logging;

    public class HubConnectionService : IHubConnectionService
    {
        private readonly IRepository<string, HubCallerContext> connectionRepository;
        private readonly IRepository<string, int> sessionIdRepository;
        private readonly IHubContext<SessionHub> sessionHubContext;
        private readonly ISessionService sessionService;
        private readonly ILogger<HubConnectionService> logger;

        public HubConnectionService(
            IRepository<string, HubCallerContext> connectionRepository,
            IRepository<string, int> sessionIdRepository,
            IHubContext<SessionHub> sessionHubContext,
            ISessionService sessionService,
            ILogger<HubConnectionService> logger)
        {
            this.connectionRepository = connectionRepository;
            this.sessionIdRepository = sessionIdRepository;
            this.sessionHubContext = sessionHubContext;
            this.sessionService = sessionService;
            this.logger = logger;
        }

        public async Task OpenConnection(
            HubCallerContext context,
            string userName,
            string accountId,
            string clientId,
            string accessToken,
            bool isSupportUser)
        {
            var session = await this.sessionService.BeginSession(userName, accountId, clientId, accessToken, isSupportUser);

            this.connectionRepository.Add(context.ConnectionId, context);
            this.sessionIdRepository.Add(context.ConnectionId, session.SessionId);
        }

        public void CloseConnection(string connectionId)
        {
            this.connectionRepository.Remove(connectionId);
            this.sessionIdRepository.Remove(connectionId);
        }

        public IEnumerable<int> GetAllConnectedSessions()
        {
            return this.sessionIdRepository.GetAll().Select(x => x.Value).Distinct();
        }

        public bool TryGetSessionId(string connectionId, out int sessionId) => this.sessionIdRepository.TryGet(connectionId, out sessionId);

        public IEnumerable<string> FindBySessionId(int sessionId)
        {
            return this.sessionIdRepository.Find(id => id == sessionId).Select(x => x.Key);
        }

        public async Task TerminateConnections(SessionActivityType reason, params string[] connectionIds)
        {
            if (reason == SessionActivityType.DifferentDeviceTermination)
            {
                await this.sessionHubContext.Clients.Clients(connectionIds)
                                   .SendAsync("concurrentSessionTermination", StatusCode.IF_ATH_502, StatusCode.IF_ATH_502.ToMessage());
            }

            foreach (var connectionId in connectionIds)
            {
                var connection = this.connectionRepository.Get(connectionId);

                if (connection == null)
                {
                    this.logger.LogWarning($"Connection with ID [{connectionId}] was not found and could not be terminated");
                }
                else
                {
                    connection.Abort();
                    this.logger.LogInformation($"Connection with ID [{connectionId}] was successfully terminated");
                }
            }
        }
    }
}
