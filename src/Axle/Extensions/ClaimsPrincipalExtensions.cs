﻿// Copyright (c) Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Extensions
{
    using System.Security.Claims;
    using Axle.Constants;

    public static class ClaimsPrincipalExtensions
    {
        public static bool IsSupportUser(this ClaimsPrincipal principal, string accountId)
        {
            var accountIdEmpty = string.IsNullOrWhiteSpace(accountId);

            var isOnBehalf = !accountIdEmpty && principal.HasClaim(Permissions.OnBehalfSelection, Permissions.OnBehalfSelection);
            var isSupport = accountIdEmpty && principal.HasClaim(Permissions.StartSessionWithoutAcc, Permissions.StartSessionWithoutAcc);

            return isOnBehalf || isSupport;
        }
    }
}