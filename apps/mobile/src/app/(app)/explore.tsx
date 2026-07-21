import { StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/ui/form-controls';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { useAuthStore } from '@/stores/auth-store';
import { useDeliveryRequestsStore } from '@/stores/delivery-requests-store';

/** Temporary account tab until Profile (16.6) lands. */
export default function AccountScreen() {
  const phoneNumber = useAuthStore((state) => state.phoneNumber);
  const role = useAuthStore((state) => state.role);
  const clearSession = useAuthStore((state) => state.clearSession);
  const clearRequests = useDeliveryRequestsStore((state) => state.clear);

  async function handleSignOut() {
    await clearRequests();
    await clearSession();
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea}>
        <ThemedView style={styles.header}>
          <ThemedText type="subtitle">Account</ThemedText>
          <ThemedText type="default" themeColor="textSecondary">
            {phoneNumber ?? 'Unknown phone'} · {role ?? 'Unknown role'}
          </ThemedText>
        </ThemedView>

        <Button label="Sign out" variant="secondary" onPress={() => void handleSignOut()} />
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
    paddingHorizontal: Spacing.four,
    paddingBottom: BottomTabInset + Spacing.three,
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    justifyContent: 'space-between',
    gap: Spacing.four,
  },
  header: {
    gap: Spacing.two,
    paddingTop: Spacing.four,
  },
});
