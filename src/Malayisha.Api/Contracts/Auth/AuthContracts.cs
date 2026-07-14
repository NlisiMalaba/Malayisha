using Malayisha.Application.Features.Auth;
using Malayisha.Domain.Enums;

namespace Malayisha.Api.Contracts.Auth;

public sealed record RegisterRequest(string PhoneNumber, UserRole Role);

public sealed record LoginRequest(string PhoneNumber);

public sealed record VerifyOtpRequest(
    string PhoneNumber,
    string OtpCode,
    OtpPurpose Purpose,
    UserRole? Role = null);

public sealed record RefreshRequest(string RefreshToken);

public sealed record OtpSentResponse(string Message);

public sealed record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    Guid UserId,
    string Role,
    string PhoneNumber);

public sealed record ErrorResponse(string ErrorCode);
