﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace Axle.Extensions
{
    using System.Security.Claims;
    using Constants;
    using IdentityModel;

    public static class ClaimsPrincipalExtensions
    {
        public static bool IsSupportUser(this ClaimsPrincipal principal, bool accountIdEmpty)
        {
            var isOnBehalf = !accountIdEmpty && principal.HasClaim(Permissions.OnBehalfSelection, Permissions.OnBehalfSelection);
            var isSupport = accountIdEmpty && principal.HasClaim(Permissions.StartSessionWithoutAcc, Permissions.StartSessionWithoutAcc);

            return isOnBehalf || isSupport;
        }

        public static string GetUsername(this ClaimsPrincipal principal) =>
            principal.FindFirst(JwtClaimTypes.Name).Value;
    }
}
