using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Auth.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IAuthRepository authRepository,
    ITokenService tokenService,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCommandHandler> logger) : IRequestHandler<RefreshTokenCommand, Result<AuthSessionResponse>>
{
    public async Task<Result<AuthSessionResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await authRepository.FindRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (storedToken is null)
        {
            return Result<AuthSessionResponse>.Error(AuthErrorCodes.InvalidRefreshToken);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var validationError = ValidateStoredToken(storedToken, nowUtc);
        if (validationError is not null)
        {
            return Result<AuthSessionResponse>.Error(validationError);
        }

        var user = await authRepository.FindUserByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result<AuthSessionResponse>.Error(AuthErrorCodes.UserNotFound);
        }

        if (!user.IsActive || user.IsDeleted)
        {
            return Result<AuthSessionResponse>.Error(AuthErrorCodes.UserInactive);
        }

        storedToken.MarkUsed(nowUtc);

        var accessToken = tokenService.CreateAccessToken(user.Id, user.PhoneNumber, user.Role);
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = tokenService.HashRefreshToken(newRefreshToken);
        var refreshExpiresAtUtc = nowUtc.AddDays(tokenService.RefreshTokenLifetimeDays);

        var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            newRefreshTokenHash,
            nowUtc,
            refreshExpiresAtUtc);

        await authRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);
        await authRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return Result<AuthSessionResponse>.Success(new AuthSessionResponse(
            accessToken.Token,
            newRefreshToken,
            accessToken.ExpiresInSeconds,
            user.Id,
            user.Role,
            user.PhoneNumber));
    }

    private static string? ValidateStoredToken(Domain.Entities.RefreshToken storedToken, DateTime nowUtc)
    {
        if (storedToken.IsRevoked)
        {
            return AuthErrorCodes.RefreshTokenRevoked;
        }

        if (storedToken.IsUsed)
        {
            return AuthErrorCodes.RefreshTokenAlreadyUsed;
        }

        if (storedToken.ExpiresAtUtc <= nowUtc)
        {
            return AuthErrorCodes.RefreshTokenExpired;
        }

        return null;
    }
}
