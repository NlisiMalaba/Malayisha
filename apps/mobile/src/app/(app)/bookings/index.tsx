import { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getApiBookings, type BookingDto } from '@/api';
import { BookingCard } from '@/components/bookings/booking-card';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { ErrorBanner, PRIMARY_COLOR } from '@/components/ui/form-controls';
import { DEFAULT_BOOKING_PAGE_SIZE } from '@/constants/bookings';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { toNumber } from '@/lib/format';

export default function BookingListScreen() {
  const [items, setItems] = useState<BookingDto[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const hasMore = items.length < totalCount;

  const loadBookings = useCallback(async (pageNumber: number, mode: 'replace' | 'append' | 'refresh') => {
    setFormError(null);
    if (mode === 'append') {
      setLoadingMore(true);
    } else if (mode === 'refresh') {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    try {
      const { data, error } = await getApiBookings({
        query: {
          Page: pageNumber,
          PageSize: DEFAULT_BOOKING_PAGE_SIZE,
        },
      });

      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to load bookings.'));
        return;
      }

      const nextItems = data.items ?? [];
      setPage(toNumber(data.page) || pageNumber);
      setTotalCount(toNumber(data.totalCount));
      setItems((current) => (mode === 'append' ? [...current, ...nextItems] : nextItems));
    } catch {
      setFormError('Unable to load bookings. Check your connection.');
    } finally {
      setLoading(false);
      setLoadingMore(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      void loadBookings(1, 'replace');
    }, [loadBookings]),
  );

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
              onRefresh={() => void loadBookings(1, 'refresh')}
              tintColor={PRIMARY_COLOR}
            />
          }
          ListHeaderComponent={
            <ThemedView style={styles.header}>
              <ThemedText type="subtitle">Bookings</ThemedText>
              <ThemedText type="default" themeColor="textSecondary">
                Track requests, quotes, and deliveries.
              </ThemedText>
              <ErrorBanner message={formError} />
              {!loading && items.length === 0 ? (
                <ThemedText type="small" themeColor="textSecondary">
                  No bookings yet. Request a trip from search to get started.
                </ThemedText>
              ) : null}
            </ThemedView>
          }
          renderItem={({ item }) => (
            <BookingCard
              booking={item}
              onPress={() => router.push(`/bookings/${item.id}`)}
            />
          )}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          ListFooterComponent={
            hasMore ? (
              <Pressable
                accessibilityRole="button"
                disabled={loadingMore}
                onPress={() => void loadBookings(page + 1, 'append')}
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
            loading ? <ActivityIndicator color={PRIMARY_COLOR} style={styles.spinner} /> : null
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
  header: {
    gap: Spacing.three,
    paddingTop: Spacing.three,
    paddingBottom: Spacing.two,
  },
  separator: {
    height: Spacing.two,
  },
  loadMore: {
    alignItems: 'center',
    paddingVertical: Spacing.three,
  },
  spinner: {
    marginTop: Spacing.four,
  },
});
