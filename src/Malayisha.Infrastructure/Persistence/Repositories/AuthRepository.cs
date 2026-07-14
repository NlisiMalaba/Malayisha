using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Malayisha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class AuthRepository(MalayishaDbContext dbContext) : IAuthRepository
{
    public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
            user => user.PhoneNumber == phoneNumber,
            cancellationToken);

    public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        dbContext.RefreshTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task AddOtpRecordAsync(OtpRecord otpRecord, CancellationToken cancellationToken = default)
    {
        await dbContext.OtpRecords.AddAsync(otpRecord, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
