import { useState } from 'react';
import { Image, Linking, ScrollView, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getApiTripsByIdShareLink } from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner } from '@/components/ui/form-controls';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { extractErrorCode, messageForAuthError } from '@/lib/auth-errors';
import { formatDateOnly, formatKg, formatRating, formatZar } from '@/lib/format';
import { useTripDetailStore } from '@/stores/trip-detail-store';

export default function TripDetailScreen() {
  const params = useLocalSearchParams<{ id?: string }>();
  const tripId = typeof params.id === 'string' ? params.id : undefined;
  const trip = useTripDetailStore((state) => state.trip);

  const [sharing, setSharing] = useState(false);
  const [shareError, setShareError] = useState<string | null>(null);

  const matchesSelection = Boolean(trip && tripId && trip.id === tripId);

  async function handleWhatsAppShare() {
    if (!tripId) {
      setShareError('Missing trip id.');
      return;
    }

    setShareError(null);
    setSharing(true);
    try {
      const { data, error } = await getApiTripsByIdShareLink({
        path: { id: tripId },
      });

      if (error || !data?.url) {
        setShareError(
          messageForAuthError(
            extractErrorCode(error),
            'Unable to build WhatsApp share link for this trip.',
          ),
        );
        return;
      }

      const canOpen = await Linking.canOpenURL(data.url);
      if (!canOpen) {
        setShareError('Unable to open WhatsApp on this device.');
        return;
      }

      await Linking.openURL(data.url);
    } catch {
      setShareError('Unable to open WhatsApp share link.');
    } finally {
      setSharing(false);
    }
  }

  if (!matchesSelection || !trip) {
    return (
      <ThemedView style={styles.container}>
        <SafeAreaView style={styles.safeArea}>
          <ThemedText type="subtitle">Trip unavailable</ThemedText>
          <ThemedText type="default" themeColor="textSecondary">
            Open a trip from search results to see its details.
          </ThemedText>
          <Button label="Back to search" variant="secondary" onPress={() => router.back()} />
        </SafeAreaView>
      </ThemedView>
    );
  }

  const { transporter } = trip;

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['bottom']}>
        <ScrollView contentContainerStyle={styles.content}>
          <ThemedView style={styles.section}>
            <ThemedText type="subtitle">
              {trip.originCity} → {trip.destinationCity}
            </ThemedText>
            {trip.isBoosted ? (
              <ThemedText type="small" themeColor="textSecondary">
                Featured listing
              </ThemedText>
            ) : null}
          </ThemedView>

          <ThemedView type="backgroundElement" style={styles.panel}>
            <DetailRow label="Departure" value={formatDateOnly(trip.departureDateUtc)} />
            <DetailRow label="Capacity" value={formatKg(trip.availableCapacityKg)} />
            <DetailRow label="Price guide" value={formatZar(trip.priceGuideZar)} />
          </ThemedView>

          <ThemedView style={styles.section}>
            <ThemedText type="smallBold">Transporter</ThemedText>
            <ThemedView type="backgroundElement" style={styles.panel}>
              <ThemedView style={styles.transporterHeader}>
                {transporter.profilePhotoUrl ? (
                  <Image
                    source={{ uri: transporter.profilePhotoUrl }}
                    style={styles.avatar}
                    accessibilityLabel={`${transporter.displayName} profile photo`}
                  />
                ) : (
                  <ThemedView type="backgroundSelected" style={styles.avatarFallback}>
                    <ThemedText type="smallBold">
                      {transporter.displayName.slice(0, 1).toUpperCase()}
                    </ThemedText>
                  </ThemedView>
                )}
                <ThemedView style={styles.transporterMeta}>
                  <ThemedText type="smallBold">{transporter.displayName}</ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {transporter.isVerified ? 'Verified oMalayisha' : 'Not verified'} · ★{' '}
                    {formatRating(transporter.averageRating)}
                  </ThemedText>
                </ThemedView>
              </ThemedView>
            </ThemedView>
          </ThemedView>

          <ErrorBanner message={shareError} />

          <Button
            label="Share on WhatsApp"
            loading={sharing}
            onPress={() => void handleWhatsAppShare()}
          />
        </ScrollView>
      </SafeAreaView>
    </ThemedView>
  );
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <ThemedView style={styles.detailRow}>
      <ThemedText type="small" themeColor="textSecondary">
        {label}
      </ThemedText>
      <ThemedText type="smallBold">{value}</ThemedText>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  safeArea: {
    flex: 1,
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    paddingHorizontal: Spacing.four,
    gap: Spacing.three,
    justifyContent: 'center',
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
    alignItems: 'center',
    gap: Spacing.two,
  },
  transporterHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
  },
  transporterMeta: {
    flex: 1,
    gap: Spacing.one,
  },
  avatar: {
    width: 56,
    height: 56,
    borderRadius: 28,
  },
  avatarFallback: {
    width: 56,
    height: 56,
    borderRadius: 28,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
