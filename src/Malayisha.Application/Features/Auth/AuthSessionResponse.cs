using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.Auth;

public sealed record AuthSessionResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    Guid UserId,
    UserRole Role,
    string PhoneNumber);
