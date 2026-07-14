namespace Malayisha.Infrastructure.Options;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public string Provider { get; set; } = "Logging";

    public string OtpMessageTemplate { get; set; } =
        "Your oMalayisha verification code is {0}. It expires in 5 minutes.";

    public TwilioSmsOptions Twilio { get; set; } = new();

    public AfricasTalkingSmsOptions AfricasTalking { get; set; } = new();
}

public sealed class TwilioSmsOptions
{
    public string AccountSid { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public string FromNumber { get; set; } = string.Empty;
}

public sealed class AfricasTalkingSmsOptions
{
    public string Username { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;
}
