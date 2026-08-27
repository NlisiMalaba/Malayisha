import { Stack } from 'expo-router';

import { useTheme } from '@/hooks/use-theme';

export default function HomeStackLayout() {
  const theme = useTheme();

  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: theme.background },
        headerTintColor: theme.text,
        headerShadowVisible: false,
        contentStyle: { backgroundColor: theme.background },
      }}>
      <Stack.Screen name="index" options={{ title: 'Trips', headerLargeTitle: true }} />
      <Stack.Screen name="[id]" options={{ title: 'Trip details' }} />
    </Stack>
  );
}
