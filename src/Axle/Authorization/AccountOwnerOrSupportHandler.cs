﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Authorization
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Constants;
    using Extensions;
    using Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;

    public class AccountOwnerOrSupportHandler : AuthorizationHandler<AccountOwnerOrSupportRequirement>
    {
        private readonly IHttpContextAccessor contextAccessor;
        private readonly IAccountsService accountsService;

        public AccountOwnerOrSupportHandler(IHttpContextAccessor contextAccessor, IAccountsService accountsService)
        {
            this.contextAccessor = contextAccessor;
            this.accountsService = accountsService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccountOwnerOrSupportRequirement requirement)
        {
            var accountId = contextAccessor.HttpContext.Request.Query["account_id"].ToString();
            var accountIdEmpty = string.IsNullOrWhiteSpace(accountId);

            var isAccountOwner = !accountIdEmpty && IsAccountOwner(context, accountId);

            if (isAccountOwner || context.User.IsSupportUser(accountIdEmpty))
            {
                // Accounts for the case where a support user tries to connect on behalf a nonexistent account
                if (accountIdEmpty || !string.IsNullOrEmpty(await accountsService.GetAccountOwnerUserName(accountId)))
                {
                    context.Succeed(requirement);
                }
            }
        }

        private bool IsAccountOwner(AuthorizationHandlerContext context, string accountId)
        {
            if (context.User.FindAll(AuthorizationClaims.Accounts).Any(scope => string.Equals(scope.Value, accountId, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }
    }
}
