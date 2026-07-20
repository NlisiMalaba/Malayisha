namespace Malayisha.Infrastructure.Notifications;

internal sealed record FcmPushMessage(
    string DeviceToken,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data);

internal sealed record FcmSendResult(bool Succeeded, string? MessageId, string? ErrorMessage)
{
    public static FcmSendResult Success(string messageId) => new(true, messageId, null);

    public static FcmSendResult Failure(string errorMessage) => new(false, null, errorMessage);
}

internal interface IFcmPushNotificationSender
{
    Task<FcmSendResult> SendAsync(
        FcmPushMessage message,
        CancellationToken cancellationToken = default);
}
