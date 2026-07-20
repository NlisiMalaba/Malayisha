namespace Malayisha.Api.Contracts.Notification;

public sealed record UpdateNotificationPreferencesRequest(bool MarketingNotificationsOptIn);

public sealed record NotificationPreferencesDto(bool MarketingNotificationsOptIn);
