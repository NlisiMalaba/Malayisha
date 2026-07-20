using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth;
using Malayisha.Domain;
using Malayisha.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Auth.DeleteAccount;

internal sealed class DeleteAccountCommandHandler(
    IAuthRepository authRepository,
    ITransporterProfileRepository profileRepository,
    TimeProvider timeProvider,
    ILogger<DeleteAccountCommandHandler> logger) : IRequestHandler<DeleteAccountCommand, Result>
{
    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await authRepository.FindUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Error(AuthErrorCodes.UserNotFound);
        }

        if (user.IsDeleted)
        {
            return Result.Error(AccountErrorCodes.AccountAlreadyDeleted);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var anonymizedPhone = AccountAnonymization.CreatePhoneIdentifier(user.Id);

        user.AnonymizeAndDelete(anonymizedPhone, nowUtc);

        var profile = await profileRepository.FindByUserIdAsync(user.Id, cancellationToken);
        if (profile is not null)
        {
            profile.Anonymize(AccountAnonymization.CreateDisplayNameIdentifier(user.Id), nowUtc);
        }

        var refreshTokens = await authRepository.ListRefreshTokensForUserAsync(user.Id, cancellationToken);
        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.Revoke(nowUtc);
        }

        await authRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Anonymised and deleted account for user {UserId}. Bookings and commission records retained.",
            user.Id);

        return Result.Success();
    }
}
