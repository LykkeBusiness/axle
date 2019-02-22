﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Hubs
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Axle.Persistence;
    using Axle.Services;
    using IdentityModel;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http.Connections;
    using Microsoft.AspNetCore.SignalR;
    using Serilog;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SessionHub : Hub
    {
        private readonly IRepository<string, HubCallerContext> connectionRepository;
        private readonly ISessionLifecycleService sessionLifecycleService;
        private readonly IAccountsService accountsService;

        public SessionHub(
            IRepository<string, HubCallerContext> connectionRepository,
            ISessionLifecycleService sessionLifecycleService,
            IAccountsService accountsService)
        {
            this.connectionRepository = connectionRepository;
            this.sessionLifecycleService = sessionLifecycleService;
            this.accountsService = accountsService;

            this.sessionLifecycleService.OnCloseConnections += this.TerminateConnections;
        }

        public static string Name => "/session";

        public override async Task OnConnectedAsync()
        {
            var sub = this.Context.User.FindFirst(JwtClaimTypes.Subject).Value;
            var clientId = this.Context.User.FindFirst("client_id").Value;

            var query = this.Context.GetHttpContext().Request.Query;

            var token = query["access_token"];
            var accountId = query["account_id"];

            if (string.IsNullOrWhiteSpace(accountId))
            {
                Log.Information($"User {sub} tried to connect without specifying account ID.");
                this.Context.Abort();
                return;
            }

            var accountOwnerId = await this.accountsService.GetAccountOwnerUserId(accountId);

            if (sub != accountOwnerId)
            {
                Log.Information($"User {sub} tried to connect with account {accountId}, which he does not own");
                this.Context.Abort();
                return;
            }

            await this.sessionLifecycleService.OpenConnection(this.Context.ConnectionId, sub, accountId, clientId, token);

            Log.Information($"New connection established. User: {sub}, ID: {this.Context.ConnectionId}.");
            this.connectionRepository.Add(this.Context.ConnectionId, this.Context);
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var sub = this.Context.User.FindFirst(JwtClaimTypes.Subject).Value;

            Log.Information($"Connection closed. User: {sub}, ID: {this.Context.ConnectionId}.");
            this.connectionRepository.Remove(this.Context.ConnectionId);
            this.sessionLifecycleService.CloseConnection(this.Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        private void TerminateConnections(IEnumerable<string> connections)
        {
            foreach (var connection in connections)
            {
                this.connectionRepository.Get(connection).Abort();
            }
        }
    }
}
