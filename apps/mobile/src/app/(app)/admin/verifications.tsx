import { useCallback, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  FlatList,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  getApiAdminVerificationsPending,
  postApiAdminVerificationsByIdApprove,
  postApiAdminVerificationsByIdReject,
  type PendingVerificationDto,
} from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, PRIMARY_COLOR, TextField } from '@/components/ui/form-controls';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { formatKg, formatRating } from '@/lib/format';

export default function PendingVerificationsScreen() {
  const [items, setItems] = useState<PendingVerificationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [actionId, setActionId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [rejectionReason, setRejectionReason] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const load = useCallback(async (mode: 'replace' | 'refresh' = 'replace') => {
    setFormError(null);
    if (mode === 'refresh') {
      setRefreshing(true);
    } else {
      setLoading(true);
    }

    try {
      const { data, error } = await getApiAdminVerificationsPending();
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to load pending verifications.'));
        return;
      }
      setItems(data);
    } catch {
      setFormError('Unable to load pending verifications. Check your connection.');
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

  async function handleApprove(id: string) {
    setFormError(null);
    setActionId(id);
    try {
      const { error } = await postApiAdminVerificationsByIdApprove({ path: { id } });
      if (error) {
        setFormError(messageForApiError(error, 'Unable to approve verification.'));
        return;
      }
      setItems((current) => current.filter((item) => item.id !== id));
    } catch {
      setFormError('Unable to approve verification. Check your connection.');
    } finally {
      setActionId(null);
    }
  }

  async function handleReject(id: string) {
    setFormError(null);
    setActionId(id);
    try {
      const { error } = await postApiAdminVerificationsByIdReject({
        path: { id },
        body: { rejectionReason: rejectionReason.trim() || null },
      });
      if (error) {
        setFormError(messageForApiError(error, 'Unable to reject verification.'));
        return;
      }
      setItems((current) => current.filter((item) => item.id !== id));
      setRejectingId(null);
      setRejectionReason('');
    } catch {
      setFormError('Unable to reject verification. Check your connection.');
    } finally {
      setActionId(null);
    }
  }

  function confirmApprove(id: string, displayName: string) {
    Alert.alert('Approve verification?', `Grant the Verified badge to ${displayName}.`, [
      { text: 'Cancel', style: 'cancel' },
      { text: 'Approve', onPress: () => void handleApprove(id) },
    ]);
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
                Pending applications, oldest first from the API.
              </ThemedText>
              <ErrorBanner message={formError} />
            </ThemedView>
          }
          renderItem={({ item }) => {
            const busy = actionId === item.id;
            const rejecting = rejectingId === item.id;
            return (
              <ThemedView type="backgroundElement" style={styles.card}>
                <ThemedText type="smallBold">{item.profile.displayName}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">
                  Routes: {item.profile.routesServed.join(', ') || 'None'}
                </ThemedText>
                <ThemedText type="small" themeColor="textSecondary">
                  {item.profile.vehicleDescription} · {formatKg(item.profile.capacityKg)} · ★{' '}
                  {formatRating(item.profile.averageRating)}
                </ThemedText>
                <ThemedText type="small" themeColor="textSecondary">
                  Submitted {new Date(item.submittedAtUtc).toLocaleString()}
                </ThemedText>

                {rejecting ? (
                  <ThemedView style={styles.rejectBox}>
                    <TextField
                      label="Rejection reason (optional)"
                      value={rejectionReason}
                      onChangeText={setRejectionReason}
                      placeholder="e.g. Documents incomplete"
                    />
                    <View style={styles.row}>
                      <Button
                        label="Confirm reject"
                        variant="secondary"
                        loading={busy}
                        onPress={() => void handleReject(item.id)}
                      />
                      <Button
                        label="Cancel"
                        variant="secondary"
                        disabled={busy}
                        onPress={() => {
                          setRejectingId(null);
                          setRejectionReason('');
                        }}
                      />
                    </View>
                  </ThemedView>
                ) : (
                  <View style={styles.row}>
                    <Button
                      label="Approve"
                      loading={busy}
                      disabled={busy}
                      onPress={() => confirmApprove(item.id, item.profile.displayName)}
                    />
                    <Button
                      label="Reject"
                      variant="secondary"
                      disabled={busy}
                      onPress={() => setRejectingId(item.id)}
                    />
                  </View>
                )}
              </ThemedView>
            );
          }}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          ListEmptyComponent={
            loading ? (
              <ActivityIndicator color={PRIMARY_COLOR} style={styles.spinner} />
            ) : (
              <ThemedText type="small" themeColor="textSecondary">
                No pending verification applications.
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
  row: {
    gap: Spacing.two,
    marginTop: Spacing.one,
  },
  rejectBox: {
    gap: Spacing.two,
    marginTop: Spacing.one,
  },
  separator: {
    height: Spacing.two,
  },
  spinner: {
    marginTop: Spacing.four,
  },
});
