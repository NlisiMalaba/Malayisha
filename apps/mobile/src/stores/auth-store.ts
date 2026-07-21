import { create } from 'zustand';

import type { AuthSessionDto } from '@/api/generated/types.gen';
import { deleteSecureItem, getSecureItem, setSecureItem } from '@/lib/secure-storage';

const ACCESS_TOKEN_KEY = 'auth.accessToken';
const REFRESH_TOKEN_KEY = 'auth.refreshToken';
const USER_ID_KEY = 'auth.userId';
const ROLE_KEY = 'auth.role';
const PHONE_KEY = 'auth.phoneNumber';

export type AuthSession = {
  accessToken: string;
  refreshToken: string;
  userId: string;
  role: string;
  phoneNumber: string;
};

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  userId: string | null;
  role: string | null;
  phoneNumber: string | null;
  isHydrated: boolean;
  isAuthenticated: boolean;
  hydrate: () => Promise<void>;
  setSession: (session: AuthSessionDto | AuthSession) => Promise<void>;
  clearSession: () => Promise<void>;
};

function toSession(session: AuthSessionDto | AuthSession): AuthSession {
  return {
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
    userId: session.userId,
    role: session.role,
    phoneNumber: session.phoneNumber,
  };
}

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  refreshToken: null,
  userId: null,
  role: null,
  phoneNumber: null,
  isHydrated: false,
  isAuthenticated: false,

  hydrate: async () => {
    const [accessToken, refreshToken, userId, role, phoneNumber] = await Promise.all([
      getSecureItem(ACCESS_TOKEN_KEY),
      getSecureItem(REFRESH_TOKEN_KEY),
      getSecureItem(USER_ID_KEY),
      getSecureItem(ROLE_KEY),
      getSecureItem(PHONE_KEY),
    ]);

    const hasSession = Boolean(accessToken && refreshToken);

    set({
      accessToken,
      refreshToken,
      userId,
      role,
      phoneNumber,
      isAuthenticated: hasSession,
      isHydrated: true,
    });
  },

  setSession: async (sessionInput) => {
    const session = toSession(sessionInput);

    await Promise.all([
      setSecureItem(ACCESS_TOKEN_KEY, session.accessToken),
      setSecureItem(REFRESH_TOKEN_KEY, session.refreshToken),
      setSecureItem(USER_ID_KEY, session.userId),
      setSecureItem(ROLE_KEY, session.role),
      setSecureItem(PHONE_KEY, session.phoneNumber),
    ]);

    set({
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      userId: session.userId,
      role: session.role,
      phoneNumber: session.phoneNumber,
      isAuthenticated: true,
      isHydrated: true,
    });
  },

  clearSession: async () => {
    await Promise.all([
      deleteSecureItem(ACCESS_TOKEN_KEY),
      deleteSecureItem(REFRESH_TOKEN_KEY),
      deleteSecureItem(USER_ID_KEY),
      deleteSecureItem(ROLE_KEY),
      deleteSecureItem(PHONE_KEY),
    ]);

    set({
      accessToken: null,
      refreshToken: null,
      userId: null,
      role: null,
      phoneNumber: null,
      isAuthenticated: false,
      isHydrated: true,
    });
  },
}));
