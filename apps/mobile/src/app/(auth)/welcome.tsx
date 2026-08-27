import { router } from 'expo-router';
import { StyleSheet } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { AuthButton } from '@/components/auth/auth-form';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { AuthMode } from '@/constants/auth';
import { MaxContentWidth, Spacing } from '@/constants/theme';

export default function WelcomeScreen() {
  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea}>
        <ThemedView style={styles.hero}>
          <ThemedText type="title" style={styles.brand}>
            Malayisha
          </ThemedText>
          <ThemedText type="default" themeColor="textSecondary" style={styles.tagline}>
            Send goods home with trusted cross-border transporters.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.actions}>
          <AuthButton
            label="Create account"
            onPress={() =>
              router.push({
                pathname: '/phone',
                params: { mode: AuthMode.Register },
              })
            }
          />
          <AuthButton
            label="Log in"
            variant="secondary"
            onPress={() =>
              router.push({
                pathname: '/phone',
                params: { mode: AuthMode.Login },
              })
            }
          />
        </ThemedView>
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
    paddingBottom: Spacing.five,
    justifyContent: 'space-between',
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
  },
  hero: {
    flex: 1,
    justifyContent: 'center',
    gap: Spacing.three,
  },
  brand: {
    fontSize: 44,
    lineHeight: 48,
  },
  tagline: {
    maxWidth: 320,
  },
  actions: {
    gap: Spacing.two,
  },
});
