using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Options;
using Malayisha.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Features.Auth.SendOtp;

internal sealed class SendOtpCommandHandler(
    IOtpStore otpStore,
    IOtpHasher otpHasher,
    IOtpGenerator otpGenerator,
    INotificationService notificationService,
    IAuthRepository authRepository,
    IOptions<AuthOtpOptions> otpOptions,
    IOptions<SmsTemplateOptions> smsOptions,
    ILogger<SendOtpCommandHandler> logger) : IRequestHandler<SendOtpCommand, Result>
{
    public async Task<Result> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        if (await otpStore.IsLockedOutAsync(request.PhoneNumber, cancellationToken))
        {
            return Result.Error(AuthErrorCodes.PhoneLockedOut);
        }

        var existingUser = await authRepository.FindUserByPhoneAsync(request.PhoneNumber, cancellationToken);

        if (request.Purpose == OtpPurpose.Register)
        {
            if (existingUser is not null)
            {
                return Result.Error(AuthErrorCodes.PhoneAlreadyRegistered);
            }
        }
        else if (existingUser is null)
        {
            return Result.Error(AuthErrorCodes.UserNotFound);
        }
        else if (!existingUser.IsActive)
        {
            return Result.Error(AuthErrorCodes.UserInactive);
        }

        var otpCode = otpGenerator.Generate();
        var otpHash = otpHasher.Hash(request.PhoneNumber, otpCode);
        var otpTtl = TimeSpan.FromSeconds(otpOptions.Value.OtpTtlSeconds);

        await otpStore.StoreHashAsync(request.PhoneNumber, otpHash, otpTtl, cancellationToken);
        await otpStore.ResetAttemptCountAsync(request.PhoneNumber, cancellationToken);

        var message = string.Format(
            smsOptions.Value.OtpMessageTemplate,
            otpCode);

        await notificationService.SendSmsAsync(request.PhoneNumber, message, cancellationToken);

        logger.LogInformation(
            "OTP dispatched for {Purpose} to phone ending {PhoneSuffix}",
            request.Purpose,
            request.PhoneNumber[^4..]);

        return Result.Success();
    }
}
