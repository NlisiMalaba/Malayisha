import { Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';

const ADMIN_LINKS = [
  {
    title: 'Pending verifications',
    subtitle: 'Approve or reject transporter badge applications.',
    href: '/admin/verifications' as const,
  },
  {
    title: 'Review moderation',
    subtitle: 'Hide or restore public reviews.',
    href: '/admin/reviews' as const,
  },
  {
    title: 'Commission report',
    subtitle: 'Track, invoice, and mark commissions paid.',
    href: '/admin/commission' as const,
  },
];

export default function AdminHomeScreen() {
  const theme = useTheme();

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['top']}>
        <ThemedView style={styles.header}>
          <ThemedText type="subtitle">Admin</ThemedText>
          <ThemedText type="default" themeColor="textSecondary">
            Manage verifications, reviews, and commissions.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.links}>
          {ADMIN_LINKS.map((link) => (
            <Pressable
              key={link.href}
              accessibilityRole="button"
              onPress={() => router.push(link.href)}
              style={({ pressed }) => [pressed && styles.pressed]}>
              <ThemedView
                type="backgroundElement"
                style={[styles.card, { borderColor: theme.backgroundSelected }]}>
                <ThemedText type="smallBold">{link.title}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">
                  {link.subtitle}
                </ThemedText>
              </ThemedView>
            </Pressable>
          ))}
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
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    paddingHorizontal: Spacing.four,
  },
  header: {
    gap: Spacing.two,
    paddingTop: Spacing.three,
    paddingBottom: Spacing.three,
  },
  links: {
    gap: Spacing.three,
    paddingBottom: BottomTabInset + Spacing.four,
  },
  card: {
    borderWidth: 1,
    borderRadius: Spacing.two,
    padding: Spacing.three,
    gap: Spacing.one,
  },
  pressed: {
    opacity: 0.85,
  },
});
