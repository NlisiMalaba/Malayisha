import { Pressable, StyleSheet } from 'react-native';

import type { TripSearchItemDto } from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PRIMARY_COLOR } from '@/components/ui/form-controls';
import { Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';
import { formatDateOnly, formatKg, formatRating, formatZar } from '@/lib/format';

type TripResultCardProps = {
  trip: TripSearchItemDto;
  onPress: () => void;
};

export function TripResultCard({ trip, onPress }: TripResultCardProps) {
  const theme = useTheme();

  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => [pressed && styles.pressed]}>
      <ThemedView
        type="backgroundElement"
        style={[styles.card, { borderColor: theme.backgroundSelected }]}>
        <ThemedView style={styles.cardHeader}>
          <ThemedText type="smallBold">
            {trip.originCity} → {trip.destinationCity}
          </ThemedText>
          {trip.isBoosted ? (
            <ThemedText type="small" style={styles.boostBadge}>
              Featured
            </ThemedText>
          ) : null}
        </ThemedView>

        <ThemedText type="small" themeColor="textSecondary">
          Departs {formatDateOnly(trip.departureDateUtc)} · {formatKg(trip.availableCapacityKg)} ·{' '}
          {formatZar(trip.priceGuideZar)}
        </ThemedText>

        <ThemedView style={styles.transporterRow}>
          <ThemedText type="small">
            {trip.transporter.displayName}
            {trip.transporter.isVerified ? ' · Verified' : ''}
          </ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            ★ {formatRating(trip.transporter.averageRating)}
          </ThemedText>
        </ThemedView>
      </ThemedView>
    </Pressable>
  );
}

type CityChipProps = {
  label: string;
  selected: boolean;
  onPress: () => void;
};

export function CityChip({ label, selected, onPress }: CityChipProps) {
  const theme = useTheme();

  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={[
        styles.chip,
        {
          backgroundColor: selected ? theme.backgroundSelected : theme.backgroundElement,
          borderColor: selected ? PRIMARY_COLOR : theme.backgroundSelected,
        },
      ]}>
      <ThemedText type="smallBold">{label}</ThemedText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: Spacing.two,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: Spacing.two,
  },
  boostBadge: {
    color: PRIMARY_COLOR,
  },
  transporterRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: Spacing.two,
  },
  pressed: {
    opacity: 0.85,
  },
  chip: {
    minHeight: 40,
    paddingHorizontal: Spacing.three,
    borderRadius: Spacing.two,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
