import { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  Switch,
  View,
} from 'react-native';
import { router } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getApiTripsSearch, type TripSearchItemDto } from '@/api';
import { CityChip, TripResultCard } from '@/components/trips/trip-result-card';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, TextField, PRIMARY_COLOR } from '@/components/ui/form-controls';
import {
  DEFAULT_TRIP_PAGE_SIZE,
  DESTINATION_CITIES,
  ORIGIN_CITIES,
  type DestinationCity,
  type OriginCity,
} from '@/constants/corridors';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { extractErrorCode, messageForAuthError } from '@/lib/auth-errors';
import { toNumber } from '@/lib/format';
import { useTripDetailStore } from '@/stores/trip-detail-store';

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export default function TripSearchScreen() {
  const setTrip = useTripDetailStore((state) => state.setTrip);

  const [originCity, setOriginCity] = useState<OriginCity>(ORIGIN_CITIES[0]);
  const [destinationCity, setDestinationCity] = useState<DestinationCity>(DESTINATION_CITIES[0]);
  const [departureDate, setDepartureDate] = useState('');
  const [maxPriceZar, setMaxPriceZar] = useState('');
  const [verifiedOnly, setVerifiedOnly] = useState(false);

  const [items, setItems] = useState<TripSearchItemDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [hasSearched, setHasSearched] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [dateError, setDateError] = useState<string | null>(null);
  const [priceError, setPriceError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const hasMore = items.length < totalCount;

  const buildQuery = useCallback(
    (pageNumber: number) => {
      const trimmedDate = departureDate.trim();
      const trimmedPrice = maxPriceZar.trim();

      if (trimmedDate && !DATE_PATTERN.test(trimmedDate)) {
        setDateError('Use yyyy-MM-dd, e.g. 2026-08-15.');
        return null;
      }
      setDateError(null);

      let parsedMaxPrice: number | undefined;
      if (trimmedPrice) {
        parsedMaxPrice = Number(trimmedPrice);
        if (!Number.isFinite(parsedMaxPrice) || parsedMaxPrice <= 0) {
          setPriceError('Enter a max price greater than 0.');
          return null;
        }
      }
      setPriceError(null);

      return {
        OriginCity: originCity,
        DestinationCity: destinationCity,
        DepartureDate: trimmedDate || undefined,
        MaxPriceZar: parsedMaxPrice,
        VerifiedOnly: verifiedOnly,
        Page: pageNumber,
        PageSize: DEFAULT_TRIP_PAGE_SIZE,
      };
    },
    [departureDate, destinationCity, maxPriceZar, originCity, verifiedOnly],
  );

  const runSearch = useCallback(
    async (pageNumber: number, mode: 'replace' | 'append' | 'refresh') => {
      const query = buildQuery(pageNumber);
      if (!query) {
        return;
      }

      setFormError(null);
      if (mode === 'append') {
        setLoadingMore(true);
      } else if (mode === 'refresh') {
        setRefreshing(true);
      } else {
        setLoading(true);
      }

      try {
        const { data, error } = await getApiTripsSearch({ query });

        if (error || !data) {
          setFormError(
            messageForAuthError(
              extractErrorCode(error),
              'Unable to search trips. Check your connection and try again.',
            ),
          );
          return;
        }

        const nextItems = data.items ?? [];
        setHasSearched(true);
        setPage(toNumber(data.page) || pageNumber);
        setTotalCount(toNumber(data.totalCount));
        setItems((current) => (mode === 'append' ? [...current, ...nextItems] : nextItems));
      } catch {
        setFormError('Unable to search trips. Check your connection and try again.');
      } finally {
        setLoading(false);
        setLoadingMore(false);
        setRefreshing(false);
      }
    },
    [buildQuery],
  );

  function openTrip(trip: TripSearchItemDto) {
    setTrip(trip);
    router.push(`/${trip.id}`);
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['top']}>
        <FlatList
          data={items}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => void runSearch(1, 'refresh')}
              tintColor={PRIMARY_COLOR}
            />
          }
          ListHeaderComponent={
            <ThemedView style={styles.filters}>
              <ThemedText type="subtitle">Find a trip</ThemedText>
              <ThemedText type="default" themeColor="textSecondary">
                Search the JHB/Pretoria → Harare/Bulawayo corridor.
              </ThemedText>

              <ThemedView style={styles.filterBlock}>
                <ThemedText type="smallBold">From</ThemedText>
                <View style={styles.chipRow}>
                  {ORIGIN_CITIES.map((city) => (
                    <CityChip
                      key={city}
                      label={city}
                      selected={originCity === city}
                      onPress={() => setOriginCity(city)}
                    />
                  ))}
                </View>
              </ThemedView>

              <ThemedView style={styles.filterBlock}>
                <ThemedText type="smallBold">To</ThemedText>
                <View style={styles.chipRow}>
                  {DESTINATION_CITIES.map((city) => (
                    <CityChip
                      key={city}
                      label={city}
                      selected={destinationCity === city}
                      onPress={() => setDestinationCity(city)}
                    />
                  ))}
                </View>
              </ThemedView>

              <TextField
                label="Departure date (optional)"
                value={departureDate}
                onChangeText={setDepartureDate}
                placeholder="yyyy-MM-dd"
                autoCapitalize="none"
                autoCorrect={false}
                error={dateError}
              />

              <TextField
                label="Max price ZAR (optional)"
                value={maxPriceZar}
                onChangeText={setMaxPriceZar}
                placeholder="e.g. 1500"
                keyboardType="decimal-pad"
                error={priceError}
              />

              <ThemedView style={styles.switchRow}>
                <ThemedText type="smallBold">Verified transporters only</ThemedText>
                <Switch
                  value={verifiedOnly}
                  onValueChange={setVerifiedOnly}
                  trackColor={{ true: PRIMARY_COLOR }}
                />
              </ThemedView>

              <ErrorBanner message={formError} />

              <Button
                label="Search trips"
                loading={loading}
                onPress={() => void runSearch(1, 'replace')}
              />

              {hasSearched ? (
                <ThemedText type="small" themeColor="textSecondary">
                  {totalCount === 0
                    ? 'No trips match these filters.'
                    : `Showing ${items.length} of ${totalCount} trips`}
                </ThemedText>
              ) : null}
            </ThemedView>
          }
          renderItem={({ item }) => (
            <TripResultCard trip={item} onPress={() => openTrip(item)} />
          )}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          ListFooterComponent={
            hasMore ? (
              <Pressable
                accessibilityRole="button"
                disabled={loadingMore}
                onPress={() => void runSearch(page + 1, 'append')}
                style={styles.loadMore}>
                {loadingMore ? (
                  <ActivityIndicator color={PRIMARY_COLOR} />
                ) : (
                  <ThemedText type="linkPrimary">Load more</ThemedText>
                )}
              </Pressable>
            ) : null
          }
          ListEmptyComponent={
            loading ? (
              <ActivityIndicator color={PRIMARY_COLOR} style={styles.emptySpinner} />
            ) : null
          }
        />
      </SafeAreaView>
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
  },
  listContent: {
    paddingHorizontal: Spacing.four,
    paddingBottom: BottomTabInset + Spacing.five,
    gap: Spacing.three,
  },
  filters: {
    gap: Spacing.three,
    paddingTop: Spacing.three,
    paddingBottom: Spacing.two,
  },
  filterBlock: {
    gap: Spacing.two,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.two,
  },
  switchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: Spacing.three,
  },
  separator: {
    height: Spacing.two,
  },
  loadMore: {
    alignItems: 'center',
    paddingVertical: Spacing.three,
  },
  emptySpinner: {
    marginTop: Spacing.four,
  },
});
