import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

const WEB_KEY_PREFIX = 'malayisha.';

/**
 * Secure token storage: Expo SecureStore on native, localStorage on web
 * (SecureStore is unavailable in browsers).
 */
export async function getSecureItem(key: string): Promise<string | null> {
  if (Platform.OS === 'web') {
    try {
      return globalThis.localStorage?.getItem(WEB_KEY_PREFIX + key) ?? null;
    } catch {
      return null;
    }
  }

  return SecureStore.getItemAsync(key);
}

export async function setSecureItem(key: string, value: string): Promise<void> {
  if (Platform.OS === 'web') {
    try {
      globalThis.localStorage?.setItem(WEB_KEY_PREFIX + key, value);
    } catch {
      // Ignore quota / private-mode failures; session stays in memory.
    }
    return;
  }

  await SecureStore.setItemAsync(key, value);
}

export async function deleteSecureItem(key: string): Promise<void> {
  if (Platform.OS === 'web') {
    try {
      globalThis.localStorage?.removeItem(WEB_KEY_PREFIX + key);
    } catch {
      // Ignore storage failures.
    }
    return;
  }

  await SecureStore.deleteItemAsync(key);
}
