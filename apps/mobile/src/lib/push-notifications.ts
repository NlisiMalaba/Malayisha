import { Platform } from 'react-native';
import * as Device from 'expo-device';
import * as Notifications from 'expo-notifications';

import { putApiNotificationsDeviceToken } from '@/api';

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
    shouldShowBanner: true,
    shouldShowList: true,
  }),
});

/**
 * Resolves the native FCM (Android) / APNs (iOS) device token via Expo Notifications.
 * Returns null on web, simulators, or when permission is denied.
 */
export async function getNativePushDeviceToken(): Promise<string | null> {
  if (Platform.OS === 'web' || !Device.isDevice) {
    return null;
  }

  const { status: existingStatus } = await Notifications.getPermissionsAsync();
  let finalStatus = existingStatus;

  if (existingStatus !== 'granted') {
    const { status } = await Notifications.requestPermissionsAsync();
    finalStatus = status;
  }

  if (finalStatus !== 'granted') {
    return null;
  }

  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('default', {
      name: 'Booking updates',
      importance: Notifications.AndroidImportance.DEFAULT,
    });
  }

  const devicePushToken = await Notifications.getDevicePushTokenAsync();
  return typeof devicePushToken.data === 'string' ? devicePushToken.data : String(devicePushToken.data);
}

/** Registers the current device push token with `PUT /api/notifications/device-token`. */
export async function registerPushDeviceTokenWithApi(): Promise<boolean> {
  try {
    const deviceToken = await getNativePushDeviceToken();
    if (!deviceToken) {
      return false;
    }

    const { error } = await putApiNotificationsDeviceToken({
      body: { deviceToken },
    });

    return !error;
  } catch {
    return false;
  }
}
