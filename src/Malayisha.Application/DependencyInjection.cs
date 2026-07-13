using Microsoft.Extensions.DependencyInjection;

namespace Malayisha.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR, FluentValidation, and use-case registration will be added in later tasks.
        return services;
    }
}
