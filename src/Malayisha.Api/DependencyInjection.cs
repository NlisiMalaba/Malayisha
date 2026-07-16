using System.Text;
using Malayisha.Api.Authorization;
using Malayisha.Api.Chat;
using Malayisha.Application.Abstractions.Chat;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Malayisha.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"JWT configuration was not found. Configure it under {JwtOptions.SectionName}.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT secret key was not configured or is too short. Set Jwt:SecretKey to at least 32 characters.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPolicies.SenderOnly, policy =>
                policy.RequireRole(UserRole.Sender.ToString()));

            options.AddPolicy(AuthPolicies.TransporterOnly, policy =>
                policy.RequireRole(UserRole.Transporter.ToString()));

            options.AddPolicy(AuthPolicies.SenderOrTransporter, policy =>
                policy.RequireRole(
                    UserRole.Sender.ToString(),
                    UserRole.Transporter.ToString()));

            options.AddPolicy(AuthPolicies.AdminOnly, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AuthPolicies.Authenticated, policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    public static IServiceCollection AddApiSignalR(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IChatNotifier, SignalRChatNotifier>();

        var redisConnectionString = configuration.GetSection(RedisOptions.SectionName)
            .Get<RedisOptions>()?.ConnectionString;

        var signalRBuilder = services.AddSignalR();

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("Malayisha");
            });
        }

        return services;
    }
}
