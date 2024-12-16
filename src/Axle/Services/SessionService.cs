﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using Dto;
    using Extensions;
    using Persistence;
    using Microsoft.Extensions.Logging;

    public sealed class SessionService : ISessionService, IDisposable
    {
        private readonly ISessionRepository sessionRepository;
        private readonly ITokenRevocationService tokenRevocationService;
        private readonly INotificationService notificationService;
        private readonly IActivityService activityService;
        private readonly IAccountsService accountsService;
        private readonly ILogger<SessionService> logger;

        private readonly SemaphoreSlim slimLock = new SemaphoreSlim(1, 1);

        public SessionService(
            ISessionRepository sessionRepository,
            ITokenRevocationService tokenRevocationService,
            INotificationService notificationService,
            IActivityService activityService,
            IAccountsService accountsService,
            ILogger<SessionService> logger)
        {
            this.sessionRepository = sessionRepository;
            this.tokenRevocationService = tokenRevocationService;
            this.notificationService = notificationService;
            this.activityService = activityService;
            this.accountsService = accountsService;
            this.logger = logger;
        }

        public async Task<Session> BeginSession(
            string userName,
            string accountId,
            string clientId,
            string accessToken,
            bool isSupportUser,
            string deviceKey)
        {
            await slimLock.WaitAsync();

            try
            {
                var existingSession = isSupportUser 
                    ? await sessionRepository.GetByUser(userName) 
                    : await sessionRepository.GetByAccount(accountId);
                
                if (existingSession?.SameDevice(deviceKey) ?? false)
                {
                    existingSession = existingSession.AddToken(accessToken);
                    await sessionRepository.Update(existingSession);
                    
                    logger.LogDebug($"Session resolved by existing cache. {nameof(userName)}: {userName}, {nameof(accountId)}: {accountId}, {nameof(existingSession.SessionId)}: {existingSession.SessionId}, {nameof(existingSession.IsSupportUser)}: {existingSession.IsSupportUser}.");
                    
                    return existingSession;
                }

                var sessionId = await GenerateSessionId();

                var newSession = new Session(userName, sessionId, accountId, accessToken, clientId, isSupportUser, deviceKey);

                await sessionRepository.Add(newSession);

                if (!newSession.IsSupportUser)
                {
                    await activityService.PublishActivity(newSession, SessionActivityType.Login);
                }
                else if (!string.IsNullOrEmpty(newSession.AccountId))
                {
                    await MakeAndPublishOnBehalfActivity(SessionActivityType.OnBehalfSupportConnected, newSession);
                }

                if (existingSession != null)
                {
                    await TerminateSession(existingSession, SessionActivityType.DifferentDeviceTermination);
                    logger.LogWarning(StatusCode.IF_ATH_502.ToMessage());
                }

                logger.LogWarning(StatusCode.IF_ATH_501.ToMessage());

                return newSession;
            }
            finally
            {
                slimLock.Release();
            }
        }

        public async Task<OnBehalfChangeResponse> UpdateOnBehalfState(int sessionId, string onBehalfAccount)
        {
            slimLock.Wait();

            try
            {
                var session = await sessionRepository.Get(sessionId);

                if (session == null)
                {
                    return OnBehalfChangeResponse.Fail($"Unknown session ID [{sessionId}]");
                }

                if (!session.IsSupportUser)
                {
                    return OnBehalfChangeResponse.Fail($"User [{session.UserName}] is not a support user");
                }

                if (session.AccountId == onBehalfAccount)
                {
                    return OnBehalfChangeResponse.Fail($"Cannot switch to the same on behalf account");
                }

                string onBehalfOwner = null;

                if (!string.IsNullOrEmpty(onBehalfAccount))
                {
                    onBehalfOwner = await accountsService.GetAccountOwnerUserName(onBehalfAccount);

                    if (string.IsNullOrEmpty(onBehalfOwner))
                    {
                        return OnBehalfChangeResponse.Fail($"Account [{onBehalfAccount}] was not found");
                    }
                }

                var newSession = new Session(session.UserName, session.SessionId, onBehalfAccount, session.AccessTokens, session.ClientId, session.IsSupportUser, session.DeviceKey);

                await sessionRepository.Update(newSession);

                if (!string.IsNullOrEmpty(session.AccountId))
                {
                    await MakeAndPublishOnBehalfActivity(SessionActivityType.OnBehalfSupportDisconnected, session);
                }

                if (!string.IsNullOrEmpty(onBehalfAccount))
                {
                    await activityService.PublishActivity(new SessionActivity(SessionActivityType.OnBehalfSupportConnected, session.SessionId, onBehalfOwner, onBehalfAccount));
                }

                return OnBehalfChangeResponse.Success();
            }
            finally
            {
                slimLock.Release();
            }
        }

        public async Task<bool> RemoveOnBehalfState(int sessionId, string onBehalfAccount)
        {
            var session = await sessionRepository.Get(sessionId);

            if (session == null || !session.IsSupportUser || session.AccountId != onBehalfAccount)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(onBehalfAccount))
            {
                var onBehalfOwner = await accountsService.GetAccountOwnerUserName(onBehalfAccount);

                if (string.IsNullOrEmpty(onBehalfOwner))
                {
                    return false;
                }
            }

            await sessionRepository.Remove(sessionId, session.UserName, session.AccountId);
            await MakeAndPublishOnBehalfActivity(SessionActivityType.OnBehalfSupportDisconnected, session);

            return true;
        }

        public async Task<TerminateSessionResponse> TerminateSession(
            string userName,
            string accountId,
            bool isSupportUser,
            SessionActivityType reason = SessionActivityType.ManualTermination)
        {
            await slimLock.WaitAsync();

            try
            {
                var userInfo = await sessionRepository.GetByUser(userName);

                if (userInfo == null && !isSupportUser && !string.IsNullOrEmpty(accountId))
                {
                    userInfo = await sessionRepository.GetByAccount(accountId);
                }

                if (userInfo == null)
                {
                    return new TerminateSessionResponse
                    {
                        Status = TerminateSessionStatus.NotFound,
                        ErrorMessage = $"No session found for the user: [{userName}] and account: [{accountId}]"
                    };
                }

                logger.LogInformation($"Terminating session: [{userInfo.SessionId}] for user: [{userName}] and account: [{accountId}]");

                await TerminateSession(userInfo, reason);

                if (reason == SessionActivityType.ManualTermination)
                {
                    logger.LogWarning(StatusCode.WN_ATH_701.ToMessage());
                }

                logger.LogInformation($"Successfully terminated session: [{userInfo.SessionId}] for user: [{userName}] and account: [{accountId}]");

                return new TerminateSessionResponse
                {
                    Status = TerminateSessionStatus.Terminated,
                    UserSessionId = userInfo.SessionId
                };
            }
            catch (Exception error)
            {
                logger.LogError(error, $"An unexpected error occurred while terminating session for user [{userName}] and account: [{accountId}]");

                return new TerminateSessionResponse
                {
                    Status = TerminateSessionStatus.Failed,
                    ErrorMessage = "An unknown error occurred while terminating user session"
                };
            }
            finally
            {
                slimLock.Release();
            }
        }

        public async Task TerminateSession(Session userInfo, SessionActivityType reason)
        {
            await sessionRepository.Remove(userInfo.SessionId, userInfo.UserName, userInfo.AccountId);

            if (reason != SessionActivityType.SignOut)
            {
                foreach (var accessToken in userInfo.AccessTokens)
                {
                    await tokenRevocationService.RevokeAccessToken(accessToken, userInfo.ClientId);    
                }
            }

            await notificationService.PublishSessionTermination(new TerminateSessionNotification() { SessionId = userInfo.SessionId, Reason = reason });

            if (!userInfo.IsSupportUser)
            {
                await activityService.PublishActivity(userInfo, reason);
            }
            else if (!string.IsNullOrEmpty(userInfo.AccountId))
            {
                await MakeAndPublishOnBehalfActivity(SessionActivityType.OnBehalfSupportDisconnected, userInfo);
            }
        }

        public async Task<int> GenerateSessionId()
        {
            var rand = new Random();
            int sessionId;

            do
            {
                sessionId = rand.Next(int.MinValue, int.MaxValue);
            }
            while (await sessionRepository.Get(sessionId) != null);

            return sessionId;
        }

        public void Dispose()
        {
            slimLock?.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task MakeAndPublishOnBehalfActivity(SessionActivityType type, Session session)
        {
            var accountOwner = await accountsService.GetAccountOwnerUserName(session.AccountId);
            var sessionActivity = new SessionActivity(type, session.SessionId, accountOwner, session.AccountId);

            await activityService.PublishActivity(sessionActivity);
        }
    }
}
