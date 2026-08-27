import { create } from 'zustand';

import type { DeliveryRequestDto } from '@/api';
import { DeliveryRequestStatusName } from '@/constants/delivery-requests';
import { deleteSecureItem, getSecureItem, setSecureItem } from '@/lib/secure-storage';

const STORAGE_KEY_PREFIX = 'deliveryRequests.';

type DeliveryRequestsState = {
  userId: string | null;
  items: DeliveryRequestDto[];
  isHydrated: boolean;
  hydrateForUser: (userId: string) => Promise<void>;
  upsert: (request: DeliveryRequestDto) => Promise<void>;
  markCancelled: (id: string) => Promise<void>;
  getById: (id: string) => DeliveryRequestDto | undefined;
  clear: () => Promise<void>;
};

function storageKey(userId: string): string {
  return `${STORAGE_KEY_PREFIX}${userId}`;
}

async function persist(userId: string, items: DeliveryRequestDto[]): Promise<void> {
  await setSecureItem(storageKey(userId), JSON.stringify(items));
}

/**
 * Caches the Sender's delivery requests locally.
 * GET /api/requests is transporter-only; create/update/cancel keep this list in sync.
 */
export const useDeliveryRequestsStore = create<DeliveryRequestsState>((set, get) => ({
  userId: null,
  items: [],
  isHydrated: false,

  hydrateForUser: async (userId) => {
    const raw = await getSecureItem(storageKey(userId));
    let items: DeliveryRequestDto[] = [];

    if (raw) {
      try {
        const parsed = JSON.parse(raw) as unknown;
        if (Array.isArray(parsed)) {
          items = parsed as DeliveryRequestDto[];
        }
      } catch {
        items = [];
      }
    }

    set({ userId, items, isHydrated: true });
  },

  upsert: async (request) => {
    const { userId, items } = get();
    if (!userId) {
      return;
    }

    const next = [request, ...items.filter((item) => item.id !== request.id)];
    set({ items: next });
    await persist(userId, next);
  },

  markCancelled: async (id) => {
    const { userId, items } = get();
    if (!userId) {
      return;
    }

    const next = items.map((item) =>
      item.id === id
        ? {
            ...item,
            status: DeliveryRequestStatusName.Cancelled as unknown as DeliveryRequestDto['status'],
            updatedAtUtc: new Date().toISOString(),
          }
        : item,
    );

    set({ items: next });
    await persist(userId, next);
  },

  getById: (id) => get().items.find((item) => item.id === id),

  clear: async () => {
    const { userId } = get();
    if (userId) {
      await deleteSecureItem(storageKey(userId));
    }
    set({ userId: null, items: [], isHydrated: false });
  },
}));
