import { useCallback, useState } from 'react';
import { ActivityIndicator, Alert, ScrollView, StyleSheet } from 'react-native';
import { router, useFocusEffect, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  getApiBookingsById,
  postApiBookingsByIdCancel,
  postApiBookingsByIdComplete,
  postApiBookingsByIdConfirm,
  postApiBookingsByIdDelivered,
  postApiBookingsByIdInTransit,
  postApiBookingsByIdQuote,
  type BookingDto,
} from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, TextField, PRIMARY_COLOR } from '@/components/ui/form-controls';
import {
  availableBookingActions,
  formatBookingStatus,
  type BookingAction,
} from '@/constants/bookings';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { formatDateOnly, formatZar, toNumber } from '@/lib/format';
import { useAuthStore } from '@/stores/auth-store';

export default function BookingDetailScreen() {
  const params = useLocalSearchParams<{ id?: string }>();
  const bookingId = typeof params.id === 'string' ? params.id : undefined;
  const role = useAuthStore((state) => state.role);

  const [booking, setBooking] = useState<BookingDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<BookingAction | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [quotePrice, setQuotePrice] = useState('');
  const [confirmPrice, setConfirmPrice] = useState('');

  const loadBooking = useCallback(async () => {
    if (!bookingId) {
      setFormError('Missing booking id.');
      setLoading(false);
      return;
    }

    setFormError(null);
    setLoading(true);
    try {
      const { data, error } = await getApiBookingsById({
        path: { id: bookingId },
      });

      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to load this booking.'));
        setBooking(null);
        return;
      }

      setBooking(data);
      if (data.quotedPriceZar != null) {
        setConfirmPrice(String(data.quotedPriceZar));
      }
    } catch {
      setFormError('Unable to load this booking. Check your connection.');
      setBooking(null);
    } finally {
      setLoading(false);
    }
  }, [bookingId]);

  useFocusEffect(
    useCallback(() => {
      void loadBooking();
    }, [loadBooking]),
  );

  const actions = booking ? availableBookingActions(booking.status, role) : [];

  async function runAction(action: BookingAction) {
    if (!bookingId) {
      return;
    }

    setFormError(null);

    if (action === 'quote') {
      const parsed = Number(quotePrice.trim());
      if (!Number.isFinite(parsed) || parsed <= 0) {
        setFormError('Enter a quote greater than 0.');
        return;
      }
    }

    if (action === 'confirm') {
      const parsed = Number(confirmPrice.trim());
      if (!Number.isFinite(parsed) || parsed <= 0) {
        setFormError('Enter an agreed price greater than 0.');
        return;
      }
    }

    if (action === 'cancel') {
      const confirmed = await confirmDestructive(
        'Cancel booking?',
        'This booking will be cancelled for both parties.',
      );
      if (!confirmed) {
        return;
      }
    }

    setActionLoading(action);
    try {
      let error: unknown;

      switch (action) {
        case 'quote':
          ({ error } = await postApiBookingsByIdQuote({
            path: { id: bookingId },
            body: { quotedPriceZar: Number(quotePrice.trim()) },
          }));
          break;
        case 'confirm':
          ({ error } = await postApiBookingsByIdConfirm({
            path: { id: bookingId },
            body: { agreedPriceZar: Number(confirmPrice.trim()) },
          }));
          break;
        case 'inTransit':
          ({ error } = await postApiBookingsByIdInTransit({ path: { id: bookingId } }));
          break;
        case 'delivered':
          ({ error } = await postApiBookingsByIdDelivered({ path: { id: bookingId } }));
          break;
        case 'complete':
          ({ error } = await postApiBookingsByIdComplete({ path: { id: bookingId } }));
          break;
        case 'cancel':
          ({ error } = await postApiBookingsByIdCancel({ path: { id: bookingId } }));
          break;
      }

      if (error) {
        setFormError(messageForApiError(error, 'Unable to update this booking.'));
        return;
      }

      await loadBooking();
    } catch {
      setFormError('Unable to update this booking. Check your connection.');
    } finally {
      setActionLoading(null);
    }
  }

  if (loading) {
    return (
      <ThemedView style={styles.container}>
        <SafeAreaView style={styles.centered}>
          <ActivityIndicator color={PRIMARY_COLOR} />
        </SafeAreaView>
      </ThemedView>
    );
  }

  if (!booking) {
    return (
      <ThemedView style={styles.container}>
        <SafeAreaView style={styles.safeArea}>
          <ThemedText type="subtitle">Booking unavailable</ThemedText>
          <ErrorBanner message={formError} />
          <Button label="Back to bookings" variant="secondary" onPress={() => router.back()} />
        </SafeAreaView>
      </ThemedView>
    );
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['bottom']}>
        <ScrollView contentContainerStyle={styles.content}>
          <ThemedView style={styles.section}>
            <ThemedText type="subtitle">{formatBookingStatus(booking.status)}</ThemedText>
            <ThemedText type="small" themeColor="textSecondary">
              Created {formatDateOnly(booking.createdAtUtc)}
            </ThemedText>
          </ThemedView>

          <ThemedView type="backgroundElement" style={styles.panel}>
            <DetailRow label="Trip" value={booking.tripListingId.slice(0, 8) + '…'} />
            {booking.quotedPriceZar != null ? (
              <DetailRow label="Quoted" value={formatZar(booking.quotedPriceZar)} />
            ) : null}
            {booking.agreedPriceZar != null ? (
              <DetailRow label="Agreed" value={formatZar(booking.agreedPriceZar)} />
            ) : null}
            {booking.message ? <DetailRow label="Message" value={booking.message} /> : null}
            {booking.inTransitAtUtc ? (
              <DetailRow label="In transit" value={formatDateOnly(booking.inTransitAtUtc)} />
            ) : null}
            {booking.deliveredAtUtc ? (
              <DetailRow label="Delivered" value={formatDateOnly(booking.deliveredAtUtc)} />
            ) : null}
            {booking.completedAtUtc ? (
              <DetailRow label="Completed" value={formatDateOnly(booking.completedAtUtc)} />
            ) : null}
            {booking.cancelledAtUtc ? (
              <DetailRow label="Cancelled" value={formatDateOnly(booking.cancelledAtUtc)} />
            ) : null}
          </ThemedView>

          {actions.includes('quote') ? (
            <TextField
              label="Quote price (ZAR)"
              value={quotePrice}
              onChangeText={setQuotePrice}
              keyboardType="decimal-pad"
              placeholder="e.g. 1200"
            />
          ) : null}

          {actions.includes('confirm') ? (
            <TextField
              label="Agreed price (ZAR)"
              value={confirmPrice}
              onChangeText={setConfirmPrice}
              keyboardType="decimal-pad"
              placeholder={
                booking.quotedPriceZar != null
                  ? String(toNumber(booking.quotedPriceZar))
                  : 'e.g. 1200'
              }
            />
          ) : null}

          <ErrorBanner message={formError} />

          <Button
            label="Open chat"
            variant="secondary"
            onPress={() => router.push(`/bookings/${booking.id}/chat`)}
          />

          <ThemedView style={styles.actions}>
            {actions.map((action) => (
              <Button
                key={action}
                label={labelForAction(action)}
                variant={action === 'cancel' ? 'secondary' : 'primary'}
                loading={actionLoading === action}
                disabled={actionLoading != null && actionLoading !== action}
                onPress={() => void runAction(action)}
              />
            ))}
          </ThemedView>
        </ScrollView>
      </SafeAreaView>
    </ThemedView>
  );
}

function labelForAction(action: BookingAction): string {
  switch (action) {
    case 'quote':
      return 'Submit quote';
    case 'confirm':
      return 'Confirm booking';
    case 'inTransit':
      return 'Mark in transit';
    case 'delivered':
      return 'Mark delivered';
    case 'complete':
      return 'Confirm delivery';
    case 'cancel':
      return 'Cancel booking';
  }
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <ThemedView style={styles.detailRow}>
      <ThemedText type="small" themeColor="textSecondary">
        {label}
      </ThemedText>
      <ThemedText type="smallBold" style={styles.detailValue}>
        {value}
      </ThemedText>
    </ThemedView>
  );
}

function confirmDestructive(title: string, message: string): Promise<boolean> {
  return new Promise((resolve) => {
    Alert.alert(title, message, [
      { text: 'Keep', style: 'cancel', onPress: () => resolve(false) },
      { text: 'Cancel booking', style: 'destructive', onPress: () => resolve(true) },
    ]);
  });
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  safeArea: {
    flex: 1,
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    paddingHorizontal: Spacing.four,
    gap: Spacing.three,
  },
  content: {
    gap: Spacing.four,
    paddingTop: Spacing.three,
    paddingBottom: BottomTabInset + Spacing.five,
  },
  section: {
    gap: Spacing.two,
  },
  panel: {
    borderRadius: Spacing.two,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: Spacing.two,
  },
  detailValue: {
    flexShrink: 1,
    textAlign: 'right',
  },
  actions: {
    gap: Spacing.two,
  },
});
