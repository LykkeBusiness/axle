﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Axle.Contracts;
    using Axle.Dto;
    using Axle.Persistence;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;
    using Serilog;
    using StackExchange.Redis;

    public sealed class SessionLifecycleService : ISessionLifecycleService, IDisposable
    {
        private readonly ISessionRepository sessionRepository;
        private readonly ITokenRevocationService tokenRevocationService;
        private readonly INotificationService notificationService;
        private readonly IActivityService activityService;
        private readonly ILogger<SessionLifecycleService> logger;
        private readonly TimeSpan sessionTimeout;

        private readonly HashSet<Action<IEnumerable<string>>> closeConnectionCallbacks = new HashSet<Action<IEnumerable<string>>>();
        private readonly Dictionary<string, Session> connectionSessionMap = new Dictionary<string, Session>();
        private readonly SemaphoreSlim slimLock = new SemaphoreSlim(1, 1);

        public SessionLifecycleService(
            ISessionRepository sessionRepository,
            ITokenRevocationService tokenRevocationService,
            INotificationService notificationService,
            IActivityService activityService,
            ILogger<SessionLifecycleService> logger,
            TimeSpan sessionTimeout)
        {
            this.sessionRepository = sessionRepository;
            this.tokenRevocationService = tokenRevocationService;
            this.notificationService = notificationService;
            this.activityService = activityService;
            this.logger = logger;
            this.sessionTimeout = sessionTimeout;

            this.notificationService.OnSessionTerminated += this.HandleSessionTermination;

            this.RefreshTimeouts();
        }

#pragma warning disable CA1710 // Event name should end in EventHandler
        public event Action<IEnumerable<string>> OnCloseConnections
        {
            add { this.closeConnectionCallbacks.Add(value); }
            remove { this.closeConnectionCallbacks.Remove(value); }
        }
#pragma warning restore CA1710 // Event name should end in EventHandler

        public void CloseConnection(string connectionId)
        {
            this.slimLock.Wait();

            try
            {
                this.connectionSessionMap.Remove(connectionId);
            }
            finally
            {
                this.slimLock.Release();
            }
        }

        public async Task OpenConnection(
            string connectionId,
            string userName,
            string accountId,
            string clientId,
            string accessToken,
            bool isSupportUser)
        {
            Session userInfo;

            await this.slimLock.WaitAsync();

            try
            {
                userInfo = this.sessionRepository.GetByUser(userName);

                if (userInfo != null && userInfo.AccessToken == accessToken)
                {
                    this.connectionSessionMap.TryAdd(connectionId, userInfo);
                    return;
                }

                var rand = new Random();
                var sessionId = 0;

                do
                {
                    sessionId = rand.Next(int.MinValue, int.MaxValue);
                }
                while (this.sessionRepository.Get(sessionId) != null);

                var newSession = new Session(userName, sessionId, accountId, accessToken, clientId, isSupportUser);

                this.sessionRepository.Add(newSession);
                this.connectionSessionMap.TryAdd(connectionId, newSession);

                if (!newSession.IsSupportUser)
                {
                    await this.activityService.PublishActivity(newSession, SessionActivityType.Login);
                }

                if (userInfo != null)
                {
                    await this.TerminateSession(userInfo, SessionActivityType.DifferentDeviceTermination);
                }
            }
            finally
            {
                this.slimLock.Release();
            }
        }

        public async Task<TerminateSessionResponse> TerminateSession(
            string userName,
            SessionActivityType reason = SessionActivityType.ManualTermination)
        {
            await this.slimLock.WaitAsync();

            try
            {
                var userInfo = this.sessionRepository.GetByUser(userName);

                if (userInfo == null)
                {
                    return new TerminateSessionResponse
                    {
                        Status = TerminateSessionStatus.NotFound,
                        ErrorMessage = $"No session found for the user: [{userName}]"
                    };
                }

                this.logger.LogInformation($"Terminating session: [{userInfo.SessionId}] for user: [{userName}]");

                await this.TerminateSession(userInfo, reason);

                this.logger.LogInformation($"Successfully terminated session: [{userInfo.SessionId}] for user: [{userName}]");

                return new TerminateSessionResponse
                {
                    Status = TerminateSessionStatus.Terminated,
                    UserSessionId = userInfo.SessionId
                };
            }
            catch (Exception error)
            {
                this.logger.LogError(error, $"An unexpected error occurred while terminating session for user [{userName}]");

                return new TerminateSessionResponse
                {
                    Status = TerminateSessionStatus.Failed,
                    ErrorMessage = "An unknown error occurred while terminating user session"
                };
            }
            finally
            {
                this.slimLock.Release();
            }
        }

        public async Task TerminateSession(Session userInfo, SessionActivityType reason)
        {
            this.sessionRepository.Remove(userInfo.SessionId, userInfo.UserName);
            await this.tokenRevocationService.RevokeAccessToken(userInfo.AccessToken, userInfo.ClientId);

            this.notificationService.PublishSessionTermination(userInfo.SessionId);

            // Support user activities are not required currently
            if (!userInfo.IsSupportUser)
            {
                await this.activityService.PublishActivity(userInfo, reason);
            }
        }

        public void Dispose()
        {
            this.slimLock?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async void RefreshTimeouts()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(this.sessionTimeout / 4);

                    await this.slimLock.WaitAsync();

                    try
                    {
                        this.sessionRepository.RefreshSessionTimeouts(this.connectionSessionMap.Values);

                        var sessionsToTerminate = this.sessionRepository.GetExpiredSessions();

                        foreach (var session in sessionsToTerminate)
                        {
                            await this.TerminateSession(session, SessionActivityType.TimeOut);
                            this.logger.LogInformation($"Successfully timed out session: [{session.SessionId}] for user: [{session.UserName}]");
                        }
                    }
                    finally
                    {
                        this.slimLock.Release();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "An error occured while trying to update session timeouts");
                }
            }
        }

        private void HandleSessionTermination(int sessionId)
        {
            this.slimLock.Wait();

            try
            {
                // Retrieve and remove the connections to the session we're closing
                var kvps = this.connectionSessionMap.Where(x => x.Value.SessionId == sessionId).ToArray();

                foreach (var kvp in kvps)
                {
                    this.connectionSessionMap.Remove(kvp.Key);
                }

                var connections = kvps.Select(x => x.Key).ToArray();

                foreach (var callback in this.closeConnectionCallbacks)
                {
                    callback(connections);
                }
            }
            finally
            {
                this.slimLock.Release();
            }
        }
    }
}
