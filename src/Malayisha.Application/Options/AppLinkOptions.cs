namespace Malayisha.Application.Options;

public sealed class AppLinkOptions
{
    public const string SectionName = "AppLinks";

    /// <summary>
    /// Base URL for trip deep links, without a trailing trip id (e.g. https://app.omalayisha.com/trips).
    /// </summary>
    public string TripDeepLinkBaseUrl { get; set; } = "https://app.omalayisha.com/trips";
}
