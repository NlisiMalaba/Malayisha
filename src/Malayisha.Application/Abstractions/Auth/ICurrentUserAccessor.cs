using Malayisha.Domain.Enums;

namespace Malayisha.Application.Abstractions.Auth;

public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    IReadOnlyCollection<UserRole> Roles { get; }
}
