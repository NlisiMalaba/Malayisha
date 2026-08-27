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
  getApiAdminCommission,
  postApiAdminCommissionByIdInvoice,
  postApiAdminCommissionByIdPaid,
  type CommissionRecordDto,
  type CommissionStatus,
} from '@/api';
import { CityChip } from '@/components/trips/trip-result-card';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, PRIMARY_COLOR, TextField } from '@/components/ui/form-controls';
import {
  CommissionStatusName,
  commissionStatusQueryValue,
  formatCommissionStatus,
  normalizeCommissionStatus,
  type CommissionStatusNameValue,
} from '@/constants/admin';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { formatDateOnly, formatZar } from '@/lib/format';

const STATUS_FILTERS: Array<CommissionStatusNameValue | 'All'> = [
  'All',
  CommissionStatusName.Pending,
  CommissionStatusName.Invoiced,
  CommissionStatusName.Paid,
];

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export default function CommissionReportScreen() {
  const [items, setItems] = useState<CommissionRecordDto[]>([]);
  const [statusFilter, setStatusFilter] = useState<CommissionStatusNameValue | 'All'>('All');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [actionId, setActionId] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const load = useCallback(
    async (mode: 'replace' | 'refresh' = 'replace') => {
      setFormError(null);

      const trimmedFrom = fromDate.trim();
      const trimmedTo = toDate.trim();
      if (trimmedFrom && !DATE_PATTERN.test(trimmedFrom)) {
        setFormError('From date must be yyyy-MM-dd.');
        return;
      }
      if (trimmedTo && !DATE_PATTERN.test(trimmedTo)) {
        setFormError('To date must be yyyy-MM-dd.');
        return;
      }

      if (mode === 'refresh') {
        setRefreshing(true);
      } else {
        setLoading(true);
      }

      try {
        const statusValue = commissionStatusQueryValue(statusFilter);
        const { data, error } = await getApiAdminCommission({
          query: {
            status: statusValue as CommissionStatus | undefined,
            fromCompletionDateUtc: trimmedFrom ? `${trimmedFrom}T00:00:00.000Z` : undefined,
            toCompletionDateUtc: trimmedTo ? `${trimmedTo}T23:59:59.999Z` : undefined,
          },
        });

        if (error || !data) {
          setFormError(messageForApiError(error, 'Unable to load commission report.'));
          return;
        }

        setItems(data.records ?? []);
      } catch {
        setFormError('Unable to load commission report. Check your connection.');
      } finally {
        setLoading(false);
        setRefreshing(false);
      }
    },
    [fromDate, statusFilter, toDate],
  );

  useFocusEffect(
    useCallback(() => {
      void load('replace');
    }, [load]),
  );

  async function handleInvoice(id: string) {
    setFormError(null);
    setActionId(id);
    try {
      const { data, error } = await postApiAdminCommissionByIdInvoice({ path: { id } });
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to mark commission as invoiced.'));
        return;
      }
      setItems((current) => current.map((item) => (item.id === id ? data : item)));
    } catch {
      setFormError('Unable to mark commission as invoiced.');
    } finally {
      setActionId(null);
    }
  }

  async function handlePaid(id: string) {
    setFormError(null);
    setActionId(id);
    try {
      const { data, error } = await postApiAdminCommissionByIdPaid({ path: { id } });
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to mark commission as paid.'));
        return;
      }
      setItems((current) => current.map((item) => (item.id === id ? data : item)));
    } catch {
      setFormError('Unable to mark commission as paid.');
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
              <ThemedText type="smallBold">Status</ThemedText>
              <View style={styles.chipRow}>
                {STATUS_FILTERS.map((status) => (
                  <CityChip
                    key={status}
                    label={status}
                    selected={statusFilter === status}
                    onPress={() => setStatusFilter(status)}
                  />
                ))}
              </View>

              <TextField
                label="From completion date (optional)"
                value={fromDate}
                onChangeText={setFromDate}
                placeholder="yyyy-MM-dd"
                autoCapitalize="none"
              />
              <TextField
                label="To completion date (optional)"
                value={toDate}
                onChangeText={setToDate}
                placeholder="yyyy-MM-dd"
                autoCapitalize="none"
              />

              <Button label="Apply filters" onPress={() => void load('replace')} />
              <ErrorBanner message={formError} />
            </ThemedView>
          }
          renderItem={({ item }) => {
            const status = normalizeCommissionStatus(item.status);
            const busy = actionId === item.id;
            return (
              <ThemedView type="backgroundElement" style={styles.card}>
                <ThemedView style={styles.cardHeader}>
                  <ThemedText type="smallBold">
                    {formatZar(item.commissionAmountZar)}
                  </ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {formatCommissionStatus(item.status)}
                  </ThemedText>
                </ThemedView>
                <ThemedText type="small" themeColor="textSecondary">
                  Agreed {formatZar(item.agreedPriceZar)} · rate{' '}
                  {(Number(item.commissionRate) * 100).toFixed(0)}%
                </ThemedText>
                <ThemedText type="small" themeColor="textSecondary">
                  Completed {formatDateOnly(item.completionDateUtc)} · transporter{' '}
                  {item.transporterUserId.slice(0, 8)}…
                </ThemedText>
                <ThemedText type="small" themeColor="textSecondary">
                  Booking {item.bookingId.slice(0, 8)}…
                </ThemedText>

                {status === CommissionStatusName.Pending ? (
                  <Button
                    label="Mark invoiced"
                    loading={busy}
                    onPress={() => void handleInvoice(item.id)}
                  />
                ) : null}
                {status === CommissionStatusName.Invoiced ? (
                  <Button
                    label="Mark paid"
                    loading={busy}
                    onPress={() => void handlePaid(item.id)}
                  />
                ) : null}
              </ThemedView>
            );
          }}
          ItemSeparatorComponent={() => <View style={styles.separator} />}
          ListEmptyComponent={
            loading ? (
              <ActivityIndicator color={PRIMARY_COLOR} style={styles.spinner} />
            ) : (
              <ThemedText type="small" themeColor="textSecondary">
                No commission records for these filters.
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
    gap: Spacing.three,
    paddingTop: Spacing.two,
    paddingBottom: Spacing.two,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.two,
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
  separator: {
    height: Spacing.two,
  },
  spinner: {
    marginTop: Spacing.four,
  },
});
