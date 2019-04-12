﻿// (c) Lykke Corporation 2019 - All rights reserved. No copying, adaptation, decompiling, distribution or any other form of use permitted.

namespace Axle.Extensions
{
    using System.Linq;
    using System.Security.Claims;
    using Axle.Constants;
    using IdentityModel;

    public static class ClaimsPrincipalExtensions
    {
        public static bool IsAuthorized(this ClaimsPrincipal claimsPrincipal, bool matchAllPermissions, params string[] permissions)
        {
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                return false;
            }

            if (permissions != null && permissions.Any())
            {
                return matchAllPermissions
                    ? permissions.All(permission => claimsPrincipal.HasClaim(permission, permission))
                    : permissions.Any(permission => claimsPrincipal.HasClaim(permission, permission));
            }

            return true;
        }

        public static bool IsSupportUser(this ClaimsPrincipal principal, string accountId)
        {
            var accountIdEmpty = string.IsNullOrWhiteSpace(accountId);

            var isOnBehalf = !accountIdEmpty && principal.HasClaim(Permissions.OnBehalfSelection, Permissions.OnBehalfSelection);
            var isSupport = accountIdEmpty && principal.HasClaim(Permissions.StartSessionWithoutAcc, Permissions.StartSessionWithoutAcc);

            return isOnBehalf || isSupport;
        }

        public static string GetUsername(this ClaimsPrincipal principal) =>
            principal.FindFirst(JwtClaimTypes.Name).Value;
    }
}
