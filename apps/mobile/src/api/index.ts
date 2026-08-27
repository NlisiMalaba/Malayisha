import { getApiBaseUrl } from './base-url';
import { configureAuthClient, refreshAccessToken } from './auth-client';
import { client } from './generated/client.gen';

client.setConfig({ baseUrl: getApiBaseUrl() });
configureAuthClient();

export { client, refreshAccessToken, getApiBaseUrl };
export * from './generated/sdk.gen';
export type * from './generated/types.gen';
