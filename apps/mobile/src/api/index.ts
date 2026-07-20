import Constants from 'expo-constants';
import { client } from './generated/client.gen';

const defaultBaseUrl =
  Constants.expoConfig?.extra?.apiBaseUrl ??
  process.env.EXPO_PUBLIC_API_BASE_URL ??
  'http://localhost:5098';

client.setConfig({ baseUrl: defaultBaseUrl });

export { client };
export * from './generated/sdk.gen';
export type * from './generated/types.gen';
