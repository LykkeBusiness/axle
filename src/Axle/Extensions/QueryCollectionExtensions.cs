using Axle.Dto;
using Microsoft.AspNetCore.Http;

namespace Axle.Extensions
{
    public static class QueryCollectionExtensions
    {
        internal static readonly string ConcurrentConnectionParameterName = "is_concurrent_connection";
        internal static readonly string AccountIdParameterName = "account_id";
        internal static readonly string AccessTokenParameterName = "access_token";
        internal static readonly string DeviceSessionKeyParameterName = "device_session_key";
        
        public static WebSocketConnectionParameters ValidateWebSocketConnection(this IQueryCollection query)
        {
            if (!bool.TryParse(query[ConcurrentConnectionParameterName], out var isConcurrentConnection))
                isConcurrentConnection = false;

            return new WebSocketConnectionParameters(
                query[AccountIdParameterName],
                query[AccessTokenParameterName],
                query[DeviceSessionKeyParameterName],
                isConcurrentConnection);
        }

        public static bool IsAccountIdEmpty(this IQueryCollection query)
        {
            var accountId = query[AccountIdParameterName];

            return string.IsNullOrWhiteSpace(accountId);
        }
    }
}