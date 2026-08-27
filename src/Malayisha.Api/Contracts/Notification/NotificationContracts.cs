namespace Malayisha.Api.Contracts.Notification;

public sealed record UpdateNotificationPreferencesRequest(bool MarketingNotificationsOptIn);

public sealed record NotificationPreferencesDto(bool MarketingNotificationsOptIn);

public sealed record RegisterPushDeviceTokenRequest(string DeviceToken);

public sealed record PushDeviceTokenDto(bool Registered);
