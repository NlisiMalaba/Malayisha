using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IAuthRepository
{
    Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddUserAsync(User user, CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> ListRefreshTokensForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task AddOtpRecordAsync(OtpRecord otpRecord, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
