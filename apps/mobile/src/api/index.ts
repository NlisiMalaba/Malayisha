import Constants from 'expo-constants';

import { configureAuthClient, refreshAccessToken } from './auth-client';
import { client } from './generated/client.gen';

const defaultBaseUrl =
  Constants.expoConfig?.extra?.apiBaseUrl ??
  process.env.EXPO_PUBLIC_API_BASE_URL ??
  'http://localhost:5098';

client.setConfig({ baseUrl: defaultBaseUrl });
configureAuthClient();

export { client, refreshAccessToken };
export * from './generated/sdk.gen';
export type * from './generated/types.gen';
