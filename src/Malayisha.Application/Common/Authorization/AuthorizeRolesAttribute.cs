using Malayisha.Domain.Enums;

namespace Malayisha.Application.Common.Authorization;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeRolesAttribute : Attribute
{
    public AuthorizeRolesAttribute(params UserRole[] roles) =>
        Roles = roles;

    public IReadOnlyCollection<UserRole> Roles { get; }
}
