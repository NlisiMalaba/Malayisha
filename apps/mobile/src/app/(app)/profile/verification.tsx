import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet } from 'react-native';
import { useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getApiProfileMe, postApiVerificationApply } from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, PRIMARY_COLOR } from '@/components/ui/form-controls';
import {
  canApplyForVerification,
  formatVerificationStatus,
  normalizeVerificationStatus,
  VerificationStatusName,
} from '@/constants/profile';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { useAuthStore } from '@/stores/auth-store';
import { useVerificationApplicationStore } from '@/stores/verification-application-store';

export default function VerificationApplicationScreen() {
  const userId = useAuthStore((state) => state.userId);
  const application = useVerificationApplicationStore((state) => state.application);
  const hydrateForUser = useVerificationApplicationStore((state) => state.hydrateForUser);
  const setApplication = useVerificationApplicationStore((state) => state.setApplication);

  const [isVerified, setIsVerified] = useState(false);
  const [loading, setLoading] = useState(true);
  const [applying, setApplying] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  const applicationStatus = normalizeVerificationStatus(application?.status);

  const load = useCallback(async () => {
    setLoading(true);
    setFormError(null);
    try {
      if (userId) {
        await hydrateForUser(userId);
      }

      const { data, error } = await getApiProfileMe();
      if (error || !data) {
        setFormError(
          messageForApiError(error, 'Create your transporter profile before applying for verification.'),
        );
        return;
      }

      setIsVerified(data.isVerified);
      if (data.isVerified) {
        setInfo('Your profile already shows the Verified oMalayisha badge.');
      }
    } catch {
      setFormError('Unable to load verification status. Check your connection.');
    } finally {
      setLoading(false);
    }
  }, [hydrateForUser, userId]);

  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  useEffect(() => {
    if (application && !isVerified) {
      setInfo(`Application status: ${formatVerificationStatus(application.status)}`);
    }
  }, [application, isVerified]);

  async function handleApply() {
    setFormError(null);
    setInfo(null);
    setApplying(true);
    try {
      const { data, error } = await postApiVerificationApply();
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to submit verification application.'));
        return;
      }

      await setApplication(data);
      setInfo(
        `Application submitted (${formatVerificationStatus(data.status)}). An admin will review it.`,
      );
    } catch {
      setFormError('Unable to submit verification application. Check your connection.');
    } finally {
      setApplying(false);
    }
  }

  const canApply = canApplyForVerification(isVerified, application?.status);

  if (loading) {
    return (
      <ThemedView style={styles.container}>
        <SafeAreaView style={styles.centered}>
          <ActivityIndicator color={PRIMARY_COLOR} />
        </SafeAreaView>
      </ThemedView>
    );
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['bottom']}>
        <ThemedView style={styles.content}>
          <ThemedText type="subtitle">Verification badge</ThemedText>
          <ThemedText type="default" themeColor="textSecondary">
            Apply for the Verified oMalayisha badge so senders can trust your profile.
          </ThemedText>

          <ThemedView type="backgroundElement" style={styles.panel}>
            <ThemedText type="smallBold">
              {isVerified
                ? 'Verified oMalayisha'
                : application
                  ? formatVerificationStatus(application.status)
                  : 'No application yet'}
            </ThemedText>
            {applicationStatus === VerificationStatusName.Rejected ? (
              <ThemedText type="small" themeColor="textSecondary">
                {application?.rejectionReason
                  ? `Rejected: ${application.rejectionReason}`
                  : 'Your previous application was rejected. You can apply again.'}
              </ThemedText>
            ) : null}
            {application && applicationStatus === VerificationStatusName.Pending ? (
              <ThemedText type="small" themeColor="textSecondary">
                Submitted {new Date(application.submittedAtUtc).toLocaleDateString()}
              </ThemedText>
            ) : null}
          </ThemedView>

          {info ? (
            <ThemedText type="small" themeColor="textSecondary">
              {info}
            </ThemedText>
          ) : null}

          <ErrorBanner message={formError} />

          <Button
            label="Apply for verification"
            loading={applying}
            disabled={!canApply}
            onPress={() => void handleApply()}
          />

          {!canApply && !isVerified ? (
            <ThemedText type="small" themeColor="textSecondary">
              You already have an active verification application.
            </ThemedText>
          ) : null}
        </ThemedView>
      </SafeAreaView>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  safeArea: {
    flex: 1,
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    paddingHorizontal: Spacing.four,
  },
  content: {
    gap: Spacing.three,
    paddingTop: Spacing.three,
  },
  panel: {
    borderRadius: Spacing.two,
    padding: Spacing.three,
    gap: Spacing.two,
  },
});
