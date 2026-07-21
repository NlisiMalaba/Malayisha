import { client } from './generated/client.gen';
import { postApiAuthRefresh } from './generated/sdk.gen';
import { useAuthStore } from '@/stores/auth-store';

const AUTH_PATH_PREFIX = '/api/auth/';

let refreshInFlight: Promise<string | null> | null = null;

function isAuthEndpoint(url: string): boolean {
  try {
    const path = new URL(url, 'http://localhost').pathname;
    return path.startsWith(AUTH_PATH_PREFIX);
  } catch {
    return url.includes(AUTH_PATH_PREFIX);
  }
}

/**
 * Rotates tokens via refresh endpoint. Concurrent 401s share one in-flight refresh.
 * Returns the new access token, or null if refresh failed (session cleared).
 */
export async function refreshAccessToken(): Promise<string | null> {
  if (refreshInFlight) {
    return refreshInFlight;
  }

  refreshInFlight = (async () => {
    const refreshToken = useAuthStore.getState().refreshToken;
    if (!refreshToken) {
      await useAuthStore.getState().clearSession();
      return null;
    }

    const { data, error } = await postApiAuthRefresh({
      body: { refreshToken },
    });

    if (error || !data) {
      await useAuthStore.getState().clearSession();
      return null;
    }

    await useAuthStore.getState().setSession(data);
    return data.accessToken;
  })().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

let authClientConfigured = false;

export function configureAuthClient(): void {
  if (authClientConfigured) {
    return;
  }
  authClientConfigured = true;

  client.setConfig({
    auth: () => useAuthStore.getState().accessToken ?? undefined,
  });

  client.interceptors.response.use(async (response, request) => {
    if (response.status !== 401 || isAuthEndpoint(request.url)) {
      return response;
    }

    const accessToken = await refreshAccessToken();
    if (!accessToken) {
      return response;
    }

    const headers = new Headers(request.headers);
    headers.set('Authorization', `Bearer ${accessToken}`);

    return fetch(new Request(request, { headers }));
  });
}
