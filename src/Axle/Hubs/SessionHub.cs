// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Axle.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Axle.Hubs
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Constants;
    using Contracts;
    using Dto;
    using Extensions;
    using Services;
    using IdentityModel;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using PermissionsManagement.Client.Extensions;
    using Serilog;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthorizationPolicies.AccountOwnerOrSupport)]
    public class SessionHub : Hub
    {
        private readonly IHubConnectionService hubConnectionService;
        private readonly ISessionService sessionService;

        public SessionHub(
            IHubConnectionService hubConnectionService,
            ISessionService sessionService)
        {
            this.hubConnectionService = hubConnectionService;
            this.sessionService = sessionService;
        }

        public static string Name => "/session";

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User.FindFirstValue(JwtClaimTypes.Name);
            if (string.IsNullOrEmpty(userName))
            {
                throw new UserClaimIsEmptyException(JwtClaimTypes.Name);
            }

            var clientId = Context.User.FindFirstValue("client_id");
            if (string.IsNullOrEmpty(clientId))
            {
                throw new UserClaimIsEmptyException("client_id");
            }

            var connectionParameters = Query.ValidateWebSocketConnection(IsSupportUser);
            
            await hubConnectionService.OpenConnection(Context, userName, clientId, connectionParameters);

            Log.Information($"New connection established. User: {userName}, ID: {Context.ConnectionId}.");
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var userName = Context.User.FindFirst(JwtClaimTypes.Name).Value;

            Log.Information($"Connection closed. User: {userName}, ID: {Context.ConnectionId}.");
            hubConnectionService.CloseConnection(Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task<bool> SignOut()
        {
            var userName = Context.User.FindFirst(JwtClaimTypes.Name).Value;
            var isSupportUser = Context.User.IsSupportUser(Query.IsAccountIdEmpty());

            var response = await sessionService.TerminateSession(userName, Query["account_id"], isSupportUser, SessionActivityType.SignOut);

            return response.Status == TerminateSessionStatus.Terminated;
        }

        public Task<OnBehalfChangeResponse> SetOnBehalfAccount(string accountId)
        {
            ThrowIfUnauthorized(matchAllPermissions: true, Permissions.OnBehalfSelection);

            if (!hubConnectionService.TryGetSessionId(Context.ConnectionId, out int sessionId))
            {
                throw new HubException("The current connection has not been registered");
            }

            return sessionService.UpdateOnBehalfState(sessionId, accountId);
        }

        public Task<bool> SetOffBehalfAccount(string accountId)
        {
            ThrowIfUnauthorized(matchAllPermissions: true, Permissions.OnBehalfSelection);

            if (!hubConnectionService.TryGetSessionId(Context.ConnectionId, out int sessionId))
            {
                throw new HubException("The current connection has not been registered");
            }

            return sessionService.RemoveOnBehalfState(sessionId, accountId);
        }

        private void ThrowIfUnauthorized(bool matchAllPermissions = false, params string[] permissions)
        {
            if (!Context.User.IsAuthorized(matchAllPermissions, permissions))
            {
                throw new HubException(
                    $"Action Forbidden ({(int)HttpStatusCode.Forbidden}). " +
                    "The user does not have the required permissions.");
            }
        }

        protected IQueryCollection Query => Context.GetHttpContext().Request.Query;

        protected bool IsSupportUser => Context.User.IsSupportUser(Query.IsAccountIdEmpty());
    }
}
