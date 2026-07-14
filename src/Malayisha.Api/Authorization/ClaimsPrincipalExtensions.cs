using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Malayisha.Api.Authorization;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }
}
