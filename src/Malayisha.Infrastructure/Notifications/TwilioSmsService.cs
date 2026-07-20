using Malayisha.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Malayisha.Infrastructure.Notifications;

internal sealed class TwilioSmsService(
    IOptions<SmsOptions> smsOptions,
    ILogger<TwilioSmsService> logger) : ISmsNotificationProvider
{
    private readonly SmsOptions _options = smsOptions.Value;
    private bool _initialized;

    public async Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var resource = await MessageResource.CreateAsync(
            to: new PhoneNumber(phoneNumber),
            from: new PhoneNumber(_options.Twilio.FromNumber),
            body: message)
            .WaitAsync(cancellationToken);

        logger.LogInformation(
            "Twilio SMS sent to phone ending {PhoneSuffix}. Status: {Status}, Sid: {Sid}",
            phoneNumber[^4..],
            resource.Status,
            resource.Sid);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Twilio.AccountSid) ||
            string.IsNullOrWhiteSpace(_options.Twilio.AuthToken))
        {
            throw new InvalidOperationException(
                "Twilio SMS provider requires Sms:Twilio:AccountSid and Sms:Twilio:AuthToken.");
        }

        TwilioClient.Init(_options.Twilio.AccountSid, _options.Twilio.AuthToken);
        _initialized = true;
    }
}
