namespace Malayisha.Infrastructure.Options;

public sealed class PushOptions
{
    public const string SectionName = "Push";

    public string Provider { get; set; } = "Logging";

    public FcmOptions Fcm { get; set; } = new();
}

public sealed class FcmOptions
{
    public string ProjectId { get; set; } = string.Empty;

    public string CredentialsPath { get; set; } = string.Empty;

    public string CredentialsJson { get; set; } = string.Empty;
}
