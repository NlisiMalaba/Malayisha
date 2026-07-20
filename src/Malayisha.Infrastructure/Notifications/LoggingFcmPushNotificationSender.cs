using Malayisha.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure.Notifications;

internal sealed class LoggingFcmPushNotificationSender(
    IOptions<PushOptions> pushOptions,
    ILogger<LoggingFcmPushNotificationSender> logger) : IFcmPushNotificationSender
{
    private readonly PushOptions _options = pushOptions.Value;

    public Task<FcmSendResult> SendAsync(
        FcmPushMessage message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Push stub dispatched via {Provider} to device ending {TokenSuffix}. Title: {Title}, Body length: {BodyLength}",
            _options.Provider,
            message.DeviceToken.Length >= 4 ? message.DeviceToken[^4..] : "****",
            message.Title,
            message.Body.Length);

        return Task.FromResult(FcmSendResult.Success("logging-stub"));
    }
}
