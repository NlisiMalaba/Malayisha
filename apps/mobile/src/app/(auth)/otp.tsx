import { useMemo, useState } from 'react';
import { Pressable, StyleSheet } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  postApiAuthLogin,
  postApiAuthRegister,
  postApiAuthVerifyOtp,
  type OtpPurpose,
  type UserRole,
} from '@/api';
import { AuthButton, AuthErrorBanner, AuthTextField } from '@/components/auth/auth-form';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import {
  AuthOtpPurpose,
  AuthRole,
  OTP_CODE_PATTERN,
  type AuthOtpPurposeValue,
  type AuthRoleValue,
} from '@/constants/auth';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import { extractErrorCode, messageForAuthError } from '@/lib/auth-errors';
import { useAuthStore } from '@/stores/auth-store';

function parsePurpose(value: string | undefined): AuthOtpPurposeValue {
  return value === AuthOtpPurpose.Register ? AuthOtpPurpose.Register : AuthOtpPurpose.Login;
}

function parseRole(value: string | undefined): AuthRoleValue | undefined {
  if (value === AuthRole.Sender || value === AuthRole.Transporter) {
    return value;
  }
  return undefined;
}

export default function OtpVerifyScreen() {
  const setSession = useAuthStore((state) => state.setSession);
  const params = useLocalSearchParams<{
    phoneNumber?: string;
    purpose?: string;
    role?: string;
  }>();

  const phoneNumber = typeof params.phoneNumber === 'string' ? params.phoneNumber : '';
  const purpose = parsePurpose(typeof params.purpose === 'string' ? params.purpose : undefined);
  const role = parseRole(typeof params.role === 'string' ? params.role : undefined);

  const [otpCode, setOtpCode] = useState('');
  const [fieldError, setFieldError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);

  const canSubmit = useMemo(() => OTP_CODE_PATTERN.test(otpCode.trim()), [otpCode]);

  async function handleVerify() {
    const code = otpCode.trim();
    setFieldError(null);
    setFormError(null);

    if (!phoneNumber) {
      setFormError('Missing phone number. Go back and try again.');
      return;
    }

    if (!OTP_CODE_PATTERN.test(code)) {
      setFieldError('Enter the 6-digit code from your SMS.');
      return;
    }

    if (purpose === AuthOtpPurpose.Register && !role) {
      setFormError('Missing role. Go back and choose Sender or Transporter.');
      return;
    }

    setLoading(true);
    try {
      const { data, error } = await postApiAuthVerifyOtp({
        body: {
          phoneNumber,
          otpCode: code,
          purpose: purpose as unknown as OtpPurpose,
          role:
            purpose === AuthOtpPurpose.Register
              ? (role as unknown as UserRole)
              : null,
        },
      });

      if (error || !data) {
        setFormError(messageForAuthError(extractErrorCode(error)));
        return;
      }

      await setSession(data);
    } catch {
      setFormError(messageForAuthError(undefined, 'Unable to verify code. Check your connection.'));
    } finally {
      setLoading(false);
    }
  }

  async function handleResend() {
    if (!phoneNumber) {
      setFormError('Missing phone number. Go back and try again.');
      return;
    }

    setFormError(null);
    setResending(true);
    try {
      if (purpose === AuthOtpPurpose.Register) {
        if (!role) {
          setFormError('Missing role. Go back and choose Sender or Transporter.');
          return;
        }

        const { error } = await postApiAuthRegister({
          body: {
            phoneNumber,
            role: role as unknown as UserRole,
          },
        });

        if (error) {
          setFormError(messageForAuthError(extractErrorCode(error)));
          return;
        }
      } else {
        const { error } = await postApiAuthLogin({
          body: { phoneNumber },
        });

        if (error) {
          setFormError(messageForAuthError(extractErrorCode(error)));
          return;
        }
      }

      setOtpCode('');
      setFieldError(null);
    } catch {
      setFormError(messageForAuthError(undefined, 'Unable to resend OTP. Check your connection.'));
    } finally {
      setResending(false);
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
            <ThemedText type="subtitle">Enter verification code</ThemedText>
            <ThemedText type="default" themeColor="textSecondary">
              We sent a 6-digit code to {phoneNumber || 'your phone'}.
            </ThemedText>
          </ThemedView>

          <AuthTextField
            label="OTP code"
            value={otpCode}
            onChangeText={(value) => setOtpCode(value.replace(/\D/g, '').slice(0, 6))}
            keyboardType="number-pad"
            textContentType="oneTimeCode"
            autoComplete="sms-otp"
            maxLength={6}
            placeholder="123456"
            error={fieldError}
            editable={!loading}
          />

          <AuthErrorBanner message={formError} />

          <Pressable
            accessibilityRole="button"
            disabled={resending || loading}
            onPress={() => void handleResend()}>
            <ThemedText type="linkPrimary">
              {resending ? 'Sending new code…' : 'Resend code'}
            </ThemedText>
          </Pressable>
        </ThemedView>

        <AuthButton
          label="Verify and continue"
          loading={loading}
          disabled={!canSubmit}
          onPress={() => void handleVerify()}
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
});
