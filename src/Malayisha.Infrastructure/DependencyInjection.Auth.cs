using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Infrastructure.Auth;
using Malayisha.Infrastructure.Options;
using Malayisha.Infrastructure.Otp;
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
        services.AddSingleton<IOtpHasher, Pbkdf2OtpHasher>();
        services.AddSingleton<IOtpGenerator, SecureOtpGenerator>();
        services.AddSingleton<ITokenService, JwtTokenService>();
    }
}
