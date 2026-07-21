import { Stack } from 'expo-router';

import { useTheme } from '@/hooks/use-theme';

export default function RequestsStackLayout() {
  const theme = useTheme();

  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: theme.background },
        headerTintColor: theme.text,
        headerShadowVisible: false,
        contentStyle: { backgroundColor: theme.background },
      }}>
      <Stack.Screen name="index" options={{ title: 'Requests', headerLargeTitle: true }} />
      <Stack.Screen
        name="create"
        options={{ title: 'Delivery request', presentation: 'card' }}
      />
    </Stack>
  );
}
