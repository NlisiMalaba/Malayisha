using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Malayisha.Api.OpenApi;

public static class OpenApiDependencyInjection
{
    public static IServiceCollection AddMalayishaOpenApi(this IServiceCollection services)
    {
        services.AddTransient<BearerSecuritySchemeTransformer>();
        services.AddTransient<AuthorizeOperationTransformer>();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Malayisha API",
                    Version = "v1",
                    Description = "oMalayisha MVP REST API."
                };

                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizeOperationTransformer>();
        });

        return services;
    }

    public static WebApplication MapMalayishaApiDocumentation(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Malayisha API");
        });

        return app;
    }
}
