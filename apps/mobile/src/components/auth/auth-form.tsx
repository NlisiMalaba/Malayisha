import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  TextInput,
  type TextInputProps,
  type PressableProps,
} from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';

const PRIMARY = '#208AEF';

type AuthButtonProps = PressableProps & {
  label: string;
  loading?: boolean;
  variant?: 'primary' | 'secondary';
};

export function AuthButton({
  label,
  loading = false,
  variant = 'primary',
  disabled,
  style,
  ...rest
}: AuthButtonProps) {
  const isPrimary = variant === 'primary';
  const isDisabled = disabled || loading;

  return (
    <Pressable
      accessibilityRole="button"
      disabled={isDisabled}
      style={(state) => [
        styles.button,
        isPrimary ? styles.buttonPrimary : styles.buttonSecondary,
        isDisabled && styles.buttonDisabled,
        state.pressed && !isDisabled && styles.buttonPressed,
        typeof style === 'function' ? style(state) : style,
      ]}
      {...rest}>
      {loading ? (
        <ActivityIndicator color={isPrimary ? '#ffffff' : PRIMARY} />
      ) : (
        <ThemedText
          type="smallBold"
          style={isPrimary ? styles.buttonPrimaryLabel : styles.buttonSecondaryLabel}>
          {label}
        </ThemedText>
      )}
    </Pressable>
  );
}

type AuthTextFieldProps = TextInputProps & {
  label: string;
  error?: string | null;
};

export function AuthTextField({ label, error, style, ...rest }: AuthTextFieldProps) {
  const theme = useTheme();

  return (
    <ThemedView style={styles.field}>
      <ThemedText type="smallBold">{label}</ThemedText>
      <TextInput
        placeholderTextColor={theme.textSecondary}
        style={[
          styles.input,
          {
            color: theme.text,
            backgroundColor: theme.backgroundElement,
            borderColor: error ? '#D14343' : theme.backgroundSelected,
          },
          style,
        ]}
        {...rest}
      />
      {error ? (
        <ThemedText type="small" style={styles.errorText}>
          {error}
        </ThemedText>
      ) : null}
    </ThemedView>
  );
}

export function AuthErrorBanner({ message }: { message: string | null }) {
  if (!message) {
    return null;
  }

  return (
    <ThemedView type="backgroundElement" style={styles.errorBanner}>
      <ThemedText type="small" style={styles.errorText}>
        {message}
      </ThemedText>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: 48,
    borderRadius: Spacing.two,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: Spacing.four,
  },
  buttonPrimary: {
    backgroundColor: PRIMARY,
  },
  buttonSecondary: {
    backgroundColor: 'transparent',
    borderWidth: 1,
    borderColor: PRIMARY,
  },
  buttonDisabled: {
    opacity: 0.5,
  },
  buttonPressed: {
    opacity: 0.85,
  },
  buttonPrimaryLabel: {
    color: '#ffffff',
  },
  buttonSecondaryLabel: {
    color: PRIMARY,
  },
  field: {
    gap: Spacing.one,
  },
  input: {
    minHeight: 48,
    borderWidth: 1,
    borderRadius: Spacing.two,
    paddingHorizontal: Spacing.three,
    fontSize: 16,
  },
  errorBanner: {
    borderRadius: Spacing.two,
    padding: Spacing.three,
  },
  errorText: {
    color: '#D14343',
  },
});
