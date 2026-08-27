import Constants from 'expo-constants';

/**
 * REST/API base URL used by the generated client and SignalR hub.
 */
export function getApiBaseUrl(): string {
  return (
    Constants.expoConfig?.extra?.apiBaseUrl ??
    process.env.EXPO_PUBLIC_API_BASE_URL ??
    'http://localhost:5098'
  );
}
