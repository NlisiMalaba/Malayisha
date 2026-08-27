import { Stack, Redirect } from 'expo-router';

import { AuthRole } from '@/constants/auth';
import { useTheme } from '@/hooks/use-theme';
import { useAuthStore } from '@/stores/auth-store';

export default function AdminStackLayout() {
  const theme = useTheme();
  const role = useAuthStore((state) => state.role);

  if (role !== AuthRole.Admin) {
    return <Redirect href="/" />;
  }

  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: theme.background },
        headerTintColor: theme.text,
        headerShadowVisible: false,
        contentStyle: { backgroundColor: theme.background },
      }}>
      <Stack.Screen name="index" options={{ title: 'Admin', headerLargeTitle: true }} />
      <Stack.Screen name="verifications" options={{ title: 'Verifications' }} />
      <Stack.Screen name="reviews" options={{ title: 'Reviews' }} />
      <Stack.Screen name="commission" options={{ title: 'Commission' }} />
    </Stack>
  );
}
