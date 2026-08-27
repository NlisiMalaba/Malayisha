import { create } from 'zustand';

import type { VerificationDto } from '@/api';
import { deleteSecureItem, getSecureItem, setSecureItem } from '@/lib/secure-storage';

const STORAGE_KEY_PREFIX = 'verificationApplication.';

type VerificationApplicationState = {
  userId: string | null;
  application: VerificationDto | null;
  isHydrated: boolean;
  hydrateForUser: (userId: string) => Promise<void>;
  setApplication: (application: VerificationDto) => Promise<void>;
  clear: () => Promise<void>;
};

function storageKey(userId: string): string {
  return `${STORAGE_KEY_PREFIX}${userId}`;
}

/**
 * Caches the transporter's latest verification application locally
 * (no GET /api/verification/me endpoint yet).
 */
export const useVerificationApplicationStore = create<VerificationApplicationState>((set, get) => ({
  userId: null,
  application: null,
  isHydrated: false,

  hydrateForUser: async (userId) => {
    const raw = await getSecureItem(storageKey(userId));
    let application: VerificationDto | null = null;

    if (raw) {
      try {
        application = JSON.parse(raw) as VerificationDto;
      } catch {
        application = null;
      }
    }

    set({ userId, application, isHydrated: true });
  },

  setApplication: async (application) => {
    const { userId } = get();
    if (!userId) {
      return;
    }

    await setSecureItem(storageKey(userId), JSON.stringify(application));
    set({ application });
  },

  clear: async () => {
    const { userId } = get();
    if (userId) {
      await deleteSecureItem(storageKey(userId));
    }
    set({ userId: null, application: null, isHydrated: false });
  },
}));
