using System.Security.Claims;
using Application.Common.Exceptions;
using Domain.Enums;

namespace API.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetRequiredUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Authenticated user id is missing.");
            }

            return userId;
        }

        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            return principal.IsInRole(UserRole.Admin.ToString());
        }
    }
}
