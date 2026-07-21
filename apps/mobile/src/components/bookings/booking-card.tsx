import { Pressable, StyleSheet } from 'react-native';

import type { BookingDto } from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { formatBookingStatus } from '@/constants/bookings';
import { Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';
import { formatDateOnly, formatZar } from '@/lib/format';

type BookingCardProps = {
  booking: BookingDto;
  onPress: () => void;
};

export function BookingCard({ booking, onPress }: BookingCardProps) {
  const theme = useTheme();
  const price =
    booking.agreedPriceZar != null || booking.quotedPriceZar != null
      ? formatZar(booking.agreedPriceZar ?? booking.quotedPriceZar ?? 0)
      : null;

  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => [pressed && styles.pressed]}>
      <ThemedView
        type="backgroundElement"
        style={[styles.card, { borderColor: theme.backgroundSelected }]}>
        <ThemedView style={styles.header}>
          <ThemedText type="smallBold">Booking</ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            {formatBookingStatus(booking.status)}
          </ThemedText>
        </ThemedView>

        <ThemedText type="small" themeColor="textSecondary">
          Updated {formatDateOnly(booking.updatedAtUtc)}
          {price ? ` · ${price}` : ''}
        </ThemedText>

        {booking.message ? (
          <ThemedText type="small" numberOfLines={2}>
            {booking.message}
          </ThemedText>
        ) : null}
      </ThemedView>
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
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: Spacing.two,
  },
  pressed: {
    opacity: 0.85,
  },
});
