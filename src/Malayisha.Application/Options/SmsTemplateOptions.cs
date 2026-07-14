namespace Malayisha.Application.Options;

public sealed class SmsTemplateOptions
{
    public const string SectionName = "Sms";

    public string OtpMessageTemplate { get; set; } =
        "Your oMalayisha verification code is {0}. It expires in 5 minutes.";
}
