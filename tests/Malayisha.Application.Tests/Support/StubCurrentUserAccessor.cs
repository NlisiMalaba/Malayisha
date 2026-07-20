using Malayisha.Application.Abstractions.Auth;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Tests.Support;

internal sealed class StubCurrentUserAccessor : ICurrentUserAccessor
{
    private StubCurrentUserAccessor(bool isAuthenticated, Guid? userId, IReadOnlyCollection<UserRole> roles)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
        Roles = roles;
    }

    public bool IsAuthenticated { get; private set; }

    public Guid? UserId { get; private set; }

    public IReadOnlyCollection<UserRole> Roles { get; private set; }

    public static StubCurrentUserAccessor Unauthenticated() =>
        new(false, null, []);

    public static StubCurrentUserAccessor Authenticated(Guid userId, params UserRole[] roles) =>
        new(true, userId, roles);
}
