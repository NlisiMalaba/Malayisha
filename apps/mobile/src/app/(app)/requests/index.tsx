import { useCallback, useEffect, useState } from 'react';
import { FlatList, StyleSheet, View } from 'react-native';
import { router, useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { postApiRequestsByIdCancel } from '@/api';
import { DeliveryRequestCard } from '@/components/requests/delivery-request-card';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner } from '@/components/ui/form-controls';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { useAuthStore } from '@/stores/auth-store';
import { useDeliveryRequestsStore } from '@/stores/delivery-requests-store';

export default function DeliveryRequestListScreen() {
  const userId = useAuthStore((state) => state.userId);
  const items = useDeliveryRequestsStore((state) => state.items);
  const isHydrated = useDeliveryRequestsStore((state) => state.isHydrated);
  const hydrateForUser = useDeliveryRequestsStore((state) => state.hydrateForUser);
  const markCancelled = useDeliveryRequestsStore((state) => state.markCancelled);

  const [cancellingId, setCancellingId] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (userId) {
      void hydrateForUser(userId);
    }
  }, [hydrateForUser, userId]);

  useFocusEffect(
    useCallback(() => {
      if (userId) {
        void hydrateForUser(userId);
      }
    }, [hydrateForUser, userId]),
  );

  async function handleCancel(id: string) {
    setFormError(null);
    setCancellingId(id);
    try {
      const { error } = await postApiRequestsByIdCancel({
        path: { id },
      });

      if (error) {
        setFormError(messageForApiError(error, 'Unable to cancel this request.'));
        return;
      }

      await markCancelled(id);
    } catch {
      setFormError('Unable to cancel this request. Check your connection.');
    } finally {
      setCancellingId(null);
    }
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['top']}>
        <FlatList
          data={items}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.listContent}
          ListHeaderComponent={
            <ThemedView style={styles.header}>
              <ThemedText type="subtitle">My requests</ThemedText>
              <ThemedText type="default" themeColor="textSecondary">
                Create a delivery request for transporters on your corridor.
              </ThemedText>
              <Button label="New request" onPress={() => router.push('/requests/create')} />
              <ErrorBanner message={formError} />
              {isHydrated && items.length === 0 ? (
                <ThemedText type="small" themeColor="textSecondary">
                  You have no delivery requests yet.
                </ThemedText>
              ) : null}
            </ThemedView>
          }
          renderItem={({ item }) => (
            <DeliveryRequestCard
              request={item}
              cancelling={cancellingId === item.id}
              onEdit={() =>
                router.push({
                  pathname: '/requests/create',
                  params: { id: item.id },
                })
              }
              onCancel={() => void handleCancel(item.id)}
            />
          )}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
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
});
