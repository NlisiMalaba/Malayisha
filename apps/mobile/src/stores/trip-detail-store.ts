import { create } from 'zustand';

import type { TripSearchItemDto } from '@/api';

type TripDetailState = {
  trip: TripSearchItemDto | null;
  setTrip: (trip: TripSearchItemDto) => void;
  clearTrip: () => void;
};

/** Holds the trip selected from search for the detail screen (no GET /trips/{id} yet). */
export const useTripDetailStore = create<TripDetailState>((set) => ({
  trip: null,
  setTrip: (trip) => set({ trip }),
  clearTrip: () => set({ trip: null }),
}));
