import { useCallback, useState } from 'react';
import { ActivityIndicator, StyleSheet, Switch, View } from 'react-native';
import { useFocusEffect } from 'expo-router';

import {
  getApiNotificationsPreferences,
  putApiNotificationsPreferences,
} from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { ErrorBanner, PRIMARY_COLOR } from '@/components/ui/form-controls';
import { AuthRole } from '@/constants/auth';
import { Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { useAuthStore } from '@/stores/auth-store';
import { useTheme } from '@/hooks/use-theme';

/**
 * Marketing push opt-in toggle wired to notification preferences APIs.
 * Booking transactional pushes are always enabled when a device token is registered.
 */
export function NotificationPreferencesSection() {
  const theme = useTheme();
  const role = useAuthStore((state) => state.role);
  const canManage =
    role === AuthRole.Sender || role === AuthRole.Transporter;

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [optIn, setOptIn] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadPreferences = useCallback(async () => {
    if (!canManage) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const { data, error: apiError } = await getApiNotificationsPreferences();
      if (apiError || !data) {
        setError(messageForApiError(apiError, 'Unable to load notification preferences.'));
        return;
      }

      setOptIn(data.marketingNotificationsOptIn);
    } catch {
      setError('Unable to load notification preferences. Check your connection.');
    } finally {
      setLoading(false);
    }
  }, [canManage]);

  useFocusEffect(
    useCallback(() => {
      void loadPreferences();
    }, [loadPreferences]),
  );

  async function handleToggle(nextValue: boolean) {
    if (saving) {
      return;
    }

    const previous = optIn;
    setOptIn(nextValue);
    setSaving(true);
    setError(null);

    try {
      const { data, error: apiError } = await putApiNotificationsPreferences({
        body: { marketingNotificationsOptIn: nextValue },
      });

      if (apiError || !data) {
        setOptIn(previous);
        setError(messageForApiError(apiError, 'Unable to update notification preferences.'));
        return;
      }

      setOptIn(data.marketingNotificationsOptIn);
    } catch {
      setOptIn(previous);
      setError('Unable to update notification preferences. Check your connection.');
    } finally {
      setSaving(false);
    }
  }

  if (!canManage) {
    return null;
  }

  return (
    <ThemedView style={styles.section}>
      <ThemedText type="smallBold">Notifications</ThemedText>
      <ThemedText type="small" themeColor="textSecondary">
        Booking updates are sent when push is enabled on this device. Marketing messages
        require an explicit opt-in.
      </ThemedText>

      {loading ? (
        <ActivityIndicator color={PRIMARY_COLOR} style={styles.loader} />
      ) : (
        <View style={styles.row}>
          <ThemedView style={styles.rowCopy}>
            <ThemedText type="default">Marketing notifications</ThemedText>
            <ThemedText type="small" themeColor="textSecondary">
              Occasional product updates and promotions
            </ThemedText>
          </ThemedView>
          <Switch
            accessibilityLabel="Marketing notifications opt-in"
            value={optIn}
            onValueChange={(value) => void handleToggle(value)}
            disabled={saving}
            trackColor={{ false: theme.backgroundSelected, true: PRIMARY_COLOR }}
            thumbColor="#ffffff"
          />
        </View>
      )}

      <ErrorBanner message={error} />
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  section: {
    gap: Spacing.two,
  },
  loader: {
    alignSelf: 'flex-start',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
  },
  rowCopy: {
    flex: 1,
    gap: Spacing.one,
  },
});
