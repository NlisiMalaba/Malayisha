namespace Malayisha.Infrastructure.Options;

public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public bool Enabled { get; set; } = true;

    public int WorkerCount { get; set; } = 5;

    public string DashboardPath { get; set; } = "/hangfire";

    public string SchemaName { get; set; } = "hangfire";
}
