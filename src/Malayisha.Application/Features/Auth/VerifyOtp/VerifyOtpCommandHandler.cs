using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Auth.Otp;
using Malayisha.Application.Options;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Features.Auth.VerifyOtp;

internal sealed class VerifyOtpCommandHandler(
    IOtpStore otpStore,
    IOtpHasher otpHasher,
    IOtpSecurityService otpSecurityService,
    IAuthRepository authRepository,
    ITokenService tokenService,
    TimeProvider timeProvider,
    IOptions<AuthOtpOptions> otpOptions,
    ILogger<VerifyOtpCommandHandler> logger) : IRequestHandler<VerifyOtpCommand, Result<AuthSessionResponse>>
{
    public async Task<Result<AuthSessionResponse>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var lockoutError = await otpSecurityService.GetLockoutErrorAsync(request.PhoneNumber, cancellationToken);
        if (lockoutError is not null)
        {
            return Result<AuthSessionResponse>.Error(lockoutError);
        }

        var storedHash = await otpStore.GetHashAsync(request.PhoneNumber, cancellationToken);
        if (storedHash is null)
        {
            return Result<AuthSessionResponse>.Error(AuthErrorCodes.OtpExpired);
        }

        if (!otpHasher.Verify(request.PhoneNumber, request.OtpCode, storedHash))
        {
            var verificationError = await otpSecurityService.RecordFailedVerificationAsync(
                request.PhoneNumber,
                cancellationToken);

            return Result<AuthSessionResponse>.Error(verificationError!);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var userResult = await ResolveUserAsync(request, nowUtc, cancellationToken);
        if (userResult.IsError)
        {
            return Result<AuthSessionResponse>.Error(userResult.ErrorCode!);
        }

        var user = userResult.Value!;

        var otpRecord = OtpRecord.Create(
            Guid.NewGuid(),
            request.PhoneNumber,
            storedHash,
            nowUtc.AddSeconds(-otpOptions.Value.OtpTtlSeconds),
            nowUtc);
        otpRecord.Consume();
        await authRepository.AddOtpRecordAsync(otpRecord, cancellationToken);

        await otpStore.RemoveAsync(request.PhoneNumber, cancellationToken);
        await otpStore.ResetAttemptCountAsync(request.PhoneNumber, cancellationToken);

        var session = await IssueSessionAsync(user, nowUtc, cancellationToken);

        logger.LogInformation(
            "OTP verified for user {UserId} via {Purpose}",
            user.Id,
            request.Purpose);

        return Result<AuthSessionResponse>.Success(session);
    }

    private async Task<Result<User>> ResolveUserAsync(
        VerifyOtpCommand request,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var existingUser = await authRepository.FindUserByPhoneAsync(request.PhoneNumber, cancellationToken);

        if (request.Purpose == OtpPurpose.Register)
        {
            if (existingUser is not null)
            {
                return Result<User>.Error(AuthErrorCodes.PhoneAlreadyRegistered);
            }

            var user = User.Create(
                Guid.NewGuid(),
                request.PhoneNumber,
                request.Role!.Value,
                nowUtc);

            await authRepository.AddUserAsync(user, cancellationToken);
            return Result<User>.Success(user);
        }

        if (existingUser is null)
        {
            return Result<User>.Error(AuthErrorCodes.UserNotFound);
        }

        if (!existingUser.IsActive)
        {
            return Result<User>.Error(AuthErrorCodes.UserInactive);
        }

        return Result<User>.Success(existingUser);
    }

    private async Task<AuthSessionResponse> IssueSessionAsync(
        User user,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user.Id, user.PhoneNumber, user.Role);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenHash = tokenService.HashRefreshToken(refreshToken);
        var refreshExpiresAtUtc = nowUtc.AddDays(tokenService.RefreshTokenLifetimeDays);

        var refreshTokenEntity = Domain.Entities.RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            refreshTokenHash,
            nowUtc,
            refreshExpiresAtUtc);

        await authRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);
        await authRepository.SaveChangesAsync(cancellationToken);

        return new AuthSessionResponse(
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresInSeconds,
            user.Id,
            user.Role,
            user.PhoneNumber);
    }
}
