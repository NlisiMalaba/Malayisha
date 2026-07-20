using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Malayisha.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure.Notifications;

internal sealed class FcmPushNotificationSender(
    IOptions<PushOptions> pushOptions,
    ILogger<FcmPushNotificationSender> logger) : IFcmPushNotificationSender
{
    private readonly PushOptions _options = pushOptions.Value;
    private bool _initialized;

    public async Task<FcmSendResult> SendAsync(
        FcmPushMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureInitialized();

            var fcmMessage = new Message
            {
                Token = message.DeviceToken,
                Notification = new Notification
                {
                    Title = message.Title,
                    Body = message.Body
                },
                Data = message.Data is null || message.Data.Count == 0
                    ? null
                    : new Dictionary<string, string>(message.Data)
            };

            var messageId = await FirebaseMessaging.DefaultInstance
                .SendAsync(fcmMessage, cancellationToken);

            return FcmSendResult.Success(messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "FCM push failed for device token ending {TokenSuffix}",
                message.DeviceToken.Length >= 4 ? message.DeviceToken[^4..] : "****");

            return FcmSendResult.Failure(ex.Message);
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        if (FirebaseApp.DefaultInstance is not null)
        {
            _initialized = true;
            return;
        }

        var credential = ResolveCredential();
        FirebaseApp.Create(new AppOptions
        {
            Credential = credential,
            ProjectId = string.IsNullOrWhiteSpace(_options.Fcm.ProjectId)
                ? null
                : _options.Fcm.ProjectId
        });

        _initialized = true;
    }

    private GoogleCredential ResolveCredential()
    {
        if (!string.IsNullOrWhiteSpace(_options.Fcm.CredentialsJson))
        {
            return CredentialFactory
                .FromJson<ServiceAccountCredential>(_options.Fcm.CredentialsJson)
                .ToGoogleCredential();
        }

        if (!string.IsNullOrWhiteSpace(_options.Fcm.CredentialsPath))
        {
            return CredentialFactory
                .FromFile<ServiceAccountCredential>(_options.Fcm.CredentialsPath)
                .ToGoogleCredential();
        }

        throw new InvalidOperationException(
            "FCM push provider requires Push:Fcm:CredentialsJson or Push:Fcm:CredentialsPath.");
    }
}
