using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Malayisha.Api.IntegrationTests;

public sealed class MalayishaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:Malayisha",
            "Host=localhost;Database=malayisha_test;Username=test;Password=test");

        builder.UseSetting("Redis:ConnectionString", "localhost:6379");
        builder.UseSetting("Jwt:SecretKey", "integration-test-secret-key-minimum-32-chars");
        builder.UseSetting("Hangfire:Enabled", "false");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Malayisha"] =
                    "Host=localhost;Database=malayisha_test;Username=test;Password=test",
                ["Redis:ConnectionString"] = "localhost:6379",
                ["Jwt:SecretKey"] = "integration-test-secret-key-minimum-32-chars",
                ["Hangfire:Enabled"] = "false"
            });
        });
    }
}
