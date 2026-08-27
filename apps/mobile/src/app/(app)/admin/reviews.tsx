import { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  getApiAdminReviews,
  postApiAdminReviewsByIdHide,
  postApiAdminReviewsByIdRestore,
  type AdminReviewDto,
} from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, PRIMARY_COLOR } from '@/components/ui/form-controls';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { formatDateOnly } from '@/lib/format';

export default function ReviewModerationScreen() {
  const [items, setItems] = useState<AdminReviewDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [actionId, setActionId] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const load = useCallback(async (mode: 'replace' | 'refresh' = 'replace') => {
    setFormError(null);
    if (mode === 'refresh') {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    try {
      const { data, error } = await getApiAdminReviews();
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to load reviews.'));
        return;
      }
      setItems(data.reviews ?? []);
    } catch {
      setFormError('Unable to load reviews. Check your connection.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useFocusEffect(
    useCallback(() => {
      void load('replace');
    }, [load]),
  );

  async function handleHide(id: string) {
    setFormError(null);
    setActionId(id);
    try {
      const { data, error } = await postApiAdminReviewsByIdHide({ path: { id } });
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to hide review.'));
        return;
      }
      setItems((current) => current.map((item) => (item.id === id ? data : item)));
    } catch {
      setFormError('Unable to hide review. Check your connection.');
    } finally {
      setActionId(null);
    }
  }

  async function handleRestore(id: string) {
    setFormError(null);
    setActionId(id);
    try {
      const { data, error } = await postApiAdminReviewsByIdRestore({ path: { id } });
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to restore review.'));
        return;
      }
      setItems((current) => current.map((item) => (item.id === id ? data : item)));
    } catch {
      setFormError('Unable to restore review. Check your connection.');
    } finally {
      setActionId(null);
    }
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['bottom']}>
        <FlatList
          data={items}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => void load('refresh')}
              tintColor={PRIMARY_COLOR}
            />
          }
          ListHeaderComponent={
            <ThemedView style={styles.header}>
              <ThemedText type="default" themeColor="textSecondary">
                All reviews including hidden ones, newest first.
              </ThemedText>
              <ErrorBanner message={formError} />
            </ThemedView>
          }
          renderItem={({ item }) => {
            const busy = actionId === item.id;
            return (
              <ThemedView type="backgroundElement" style={styles.card}>
                <ThemedView style={styles.cardHeader}>
                  <ThemedText type="smallBold">★ {String(item.rating)}</ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {item.isHidden ? 'Hidden' : 'Visible'}
                  </ThemedText>
                </ThemedView>
                <ThemedText type="small" themeColor="textSecondary">
                  {formatDateOnly(item.createdAtUtc)} · booking {item.bookingId.slice(0, 8)}…
                </ThemedText>
                <ThemedText type="small">
                  {item.comment?.trim() ? item.comment : 'No comment'}
                </ThemedText>
                <View style={styles.row}>
                  {item.isHidden ? (
                    <Button
                      label="Restore"
                      loading={busy}
                      onPress={() => void handleRestore(item.id)}
                    />
                  ) : (
                    <Button
                      label="Hide"
                      variant="secondary"
                      loading={busy}
                      onPress={() => void handleHide(item.id)}
                    />
                  )}
                </View>
              </ThemedView>
            );
          }}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          ListEmptyComponent={
            loading ? (
              <ActivityIndicator color={PRIMARY_COLOR} style={styles.spinner} />
            ) : (
              <ThemedText type="small" themeColor="textSecondary">
                No reviews to moderate.
              </ThemedText>
            )
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
    gap: Spacing.two,
    paddingTop: Spacing.two,
  },
  card: {
    borderRadius: Spacing.two,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: Spacing.two,
  },
  row: {
    marginTop: Spacing.one,
  },
  separator: {
    height: Spacing.two,
  },
  spinner: {
    marginTop: Spacing.four,
  },
});
