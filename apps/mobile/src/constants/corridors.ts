/** Beachhead corridor cities for Sender trip search (JHB/Pretoria → Harare/Bulawayo). */
export const ORIGIN_CITIES = ['Johannesburg', 'Pretoria'] as const;
export const DESTINATION_CITIES = ['Harare', 'Bulawayo'] as const;

export type OriginCity = (typeof ORIGIN_CITIES)[number];
export type DestinationCity = (typeof DESTINATION_CITIES)[number];

export const DEFAULT_TRIP_PAGE_SIZE = 20;
