using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Chat;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Infrastructure.Auth;
using Malayisha.Infrastructure.Options;
using Malayisha.Infrastructure.Otp;
using Malayisha.Infrastructure.Chat;
using Malayisha.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Malayisha.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddAuthServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<ITransporterProfileRepository, TransporterProfileRepository>();
        services.AddScoped<IVerificationRepository, VerificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ITripListingRepository, TripListingRepository>();
        services.AddScoped<IDeliveryRequestRepository, DeliveryRequestRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ICommissionRecordRepository, CommissionRecordRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddSingleton<IChatPresenceTracker, RedisChatPresenceTracker>();
        services.AddSingleton<IOtpHasher, Pbkdf2OtpHasher>();
        services.AddSingleton<IOtpGenerator, SecureOtpGenerator>();
        services.AddSingleton<ITokenService, JwtTokenService>();
    }
}
