using FluentValidation;
using Malayisha.Application.Behaviors;
using Malayisha.Application.Features.Auth.Otp;
using Malayisha.Application.Options;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Malayisha.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOtpOptions>(configuration.GetSection(AuthOtpOptions.SectionName));
        services.Configure<SmsTemplateOptions>(configuration.GetSection(SmsTemplateOptions.SectionName));

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        services.AddSingleton<IOtpSecurityService, OtpSecurityService>();

        return services;
    }
}
