import { useMemo, useState } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { postApiAuthLogin, postApiAuthRegister, type UserRole } from '@/api';
import { AuthButton, AuthErrorBanner, AuthTextField } from '@/components/auth/auth-form';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import {
  AuthMode,
  AuthOtpPurpose,
  AuthRole,
  type AuthModeValue,
  type AuthRoleValue,
  PHONE_E164_PATTERN,
} from '@/constants/auth';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';
import { extractErrorCode, messageForAuthError } from '@/lib/auth-errors';

function normalizePhone(raw: string): string {
  const trimmed = raw.trim().replace(/[\s()-]/g, '');
  if (trimmed.startsWith('00')) {
    return `+${trimmed.slice(2)}`;
  }
  return trimmed;
}

export default function PhoneEntryScreen() {
  const theme = useTheme();
  const params = useLocalSearchParams<{ mode?: string }>();
  const mode: AuthModeValue =
    params.mode === AuthMode.Login ? AuthMode.Login : AuthMode.Register;

  const [phoneNumber, setPhoneNumber] = useState('+27');
  const [role, setRole] = useState<AuthRoleValue>(AuthRole.Sender);
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const title = mode === AuthMode.Register ? 'Create your account' : 'Welcome back';
  const subtitle =
    mode === AuthMode.Register
      ? 'Enter your mobile number and choose how you will use Malayisha.'
      : 'Enter the phone number linked to your account.';

  const canSubmit = useMemo(() => PHONE_E164_PATTERN.test(normalizePhone(phoneNumber)), [phoneNumber]);

  async function handleContinue() {
    const normalized = normalizePhone(phoneNumber);
    setFieldError(null);
    setFormError(null);

    if (!PHONE_E164_PATTERN.test(normalized)) {
      setFieldError('Enter a valid phone number in E.164 format, e.g. +27821234567.');
      return;
    }

    setLoading(true);
    try {
      if (mode === AuthMode.Register) {
        const { error } = await postApiAuthRegister({
          body: {
            phoneNumber: normalized,
            role: role as unknown as UserRole,
          },
        });

        if (error) {
          setFormError(messageForAuthError(extractErrorCode(error)));
          return;
        }

        router.push({
          pathname: '/otp',
          params: {
            phoneNumber: normalized,
            purpose: AuthOtpPurpose.Register,
            role,
          },
        });
        return;
      }

      const { error } = await postApiAuthLogin({
        body: { phoneNumber: normalized },
      });

      if (error) {
        setFormError(messageForAuthError(extractErrorCode(error)));
        return;
      }

      router.push({
        pathname: '/otp',
        params: {
          phoneNumber: normalized,
          purpose: AuthOtpPurpose.Login,
        },
      });
    } catch {
      setFormError(messageForAuthError(undefined, 'Unable to send OTP. Check your connection.'));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea}>
        <ThemedView style={styles.content}>
          <Pressable onPress={() => router.back()} accessibilityRole="button">
            <ThemedText type="linkPrimary">Back</ThemedText>
          </Pressable>

          <ThemedView style={styles.header}>
            <ThemedText type="subtitle">{title}</ThemedText>
            <ThemedText type="default" themeColor="textSecondary">
              {subtitle}
            </ThemedText>
          </ThemedView>

          <AuthTextField
            label="Phone number"
            value={phoneNumber}
            onChangeText={setPhoneNumber}
            keyboardType="phone-pad"
            autoComplete="tel"
            textContentType="telephoneNumber"
            placeholder="+27821234567"
            error={fieldError}
            editable={!loading}
          />

          {mode === AuthMode.Register ? (
            <ThemedView style={styles.roleSection}>
              <ThemedText type="smallBold">I am a</ThemedText>
              <ThemedView style={styles.roleRow}>
                {(Object.values(AuthRole) as AuthRoleValue[]).map((option) => {
                  const selected = role === option;
                  return (
                    <Pressable
                      key={option}
                      accessibilityRole="button"
                      disabled={loading}
                      onPress={() => setRole(option)}
                      style={[
                        styles.roleChip,
                        {
                          backgroundColor: selected
                            ? theme.backgroundSelected
                            : theme.backgroundElement,
                          borderColor: selected ? '#208AEF' : theme.backgroundSelected,
                        },
                      ]}>
                      <ThemedText type="smallBold">{option}</ThemedText>
                    </Pressable>
                  );
                })}
              </ThemedView>
            </ThemedView>
          ) : null}

          <AuthErrorBanner message={formError} />
        </ThemedView>

        <AuthButton
          label="Send code"
          loading={loading}
          disabled={!canSubmit}
          onPress={() => void handleContinue()}
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
    paddingHorizontal: Spacing.four,
    paddingBottom: Spacing.five,
    justifyContent: 'space-between',
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    gap: Spacing.four,
  },
  content: {
    gap: Spacing.four,
    paddingTop: Spacing.three,
  },
  header: {
    gap: Spacing.two,
  },
  roleSection: {
    gap: Spacing.two,
  },
  roleRow: {
    flexDirection: 'row',
    gap: Spacing.two,
  },
  roleChip: {
    flex: 1,
    minHeight: 44,
    borderWidth: 1,
    borderRadius: Spacing.two,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: Spacing.two,
  },
});
