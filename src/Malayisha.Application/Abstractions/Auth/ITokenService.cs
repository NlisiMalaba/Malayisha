using Malayisha.Domain.Enums;

namespace Malayisha.Application.Abstractions.Auth;

public sealed record AccessTokenResult(string Token, int ExpiresInSeconds);

public interface ITokenService
{
    int RefreshTokenLifetimeDays { get; }

    AccessTokenResult CreateAccessToken(Guid userId, string phoneNumber, UserRole role);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
