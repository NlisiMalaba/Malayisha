using System.Net.Http.Headers;
using System.Text;
using Malayisha.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure.Notifications;

internal sealed class AfricasTalkingSmsService(
    IHttpClientFactory httpClientFactory,
    IOptions<SmsOptions> smsOptions,
    ILogger<AfricasTalkingSmsService> logger) : ISmsNotificationProvider
{
    private readonly SmsOptions _options = smsOptions.Value;

    public async Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AfricasTalking.Username) ||
            string.IsNullOrWhiteSpace(_options.AfricasTalking.ApiKey))
        {
            throw new InvalidOperationException(
                "Africa's Talking SMS provider requires Sms:AfricasTalking:Username and Sms:AfricasTalking:ApiKey.");
        }

        using var client = httpClientFactory.CreateClient(nameof(AfricasTalkingSmsService));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("apiKey", _options.AfricasTalking.ApiKey);

        var payload = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = _options.AfricasTalking.Username,
            ["to"] = phoneNumber,
            ["message"] = message,
            ["from"] = _options.AfricasTalking.From
        });

        using var response = await client.PostAsync(
            "https://api.africastalking.com/version1/messaging",
            payload,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Africa's Talking SMS failed for phone ending {PhoneSuffix}. Status: {StatusCode}, Body: {Body}",
                phoneNumber[^4..],
                (int)response.StatusCode,
                body);

            response.EnsureSuccessStatusCode();
        }

        logger.LogInformation(
            "Africa's Talking SMS sent to phone ending {PhoneSuffix}",
            phoneNumber[^4..]);
    }
}
