using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Malayisha.Application.Abstractions.Auth;
using Malayisha.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Malayisha.Api.Authorization;

internal sealed class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserAccessor
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return null;
            }

            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue("sub");

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public IReadOnlyCollection<UserRole> Roles
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return [];
            }

            return principal.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .Select(value => Enum.TryParse<UserRole>(value, out var role) ? role : (UserRole?)null)
                .Where(role => role is not null)
                .Select(role => role!.Value)
                .Distinct()
                .ToArray();
        }
    }
}
