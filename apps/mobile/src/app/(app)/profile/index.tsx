import { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Image,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { router, useFocusEffect } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import {
  getApiProfileMe,
  postApiProfile,
  postApiProfilePhoto,
  putApiProfile,
  type TransporterProfileDto,
} from '@/api';
import { CityChip } from '@/components/trips/trip-result-card';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, TextField, PRIMARY_COLOR } from '@/components/ui/form-controls';
import { AuthRole } from '@/constants/auth';
import { PROFILE_PHOTO_CONTENT_TYPES, PROFILE_ROUTE_OPTIONS } from '@/constants/profile';
import { BottomTabInset, MaxContentWidth, Spacing } from '@/constants/theme';
import { extractErrorCode } from '@/lib/auth-errors';
import { messageForApiError } from '@/lib/api-errors';
import { formatRating } from '@/lib/format';
import { useAuthStore } from '@/stores/auth-store';
import { useDeliveryRequestsStore } from '@/stores/delivery-requests-store';
import { useVerificationApplicationStore } from '@/stores/verification-application-store';

function extensionForContentType(contentType: string): string {
  switch (contentType) {
    case 'image/png':
      return 'png';
    case 'image/webp':
      return 'webp';
    default:
      return 'jpg';
  }
}

export default function ProfileScreen() {
  const role = useAuthStore((state) => state.role);
  const phoneNumber = useAuthStore((state) => state.phoneNumber);
  const userId = useAuthStore((state) => state.userId);
  const clearSession = useAuthStore((state) => state.clearSession);
  const clearRequests = useDeliveryRequestsStore((state) => state.clear);
  const clearVerification = useVerificationApplicationStore((state) => state.clear);

  const isTransporter = role === AuthRole.Transporter;

  const [profile, setProfile] = useState<TransporterProfileDto | null>(null);
  const [loading, setLoading] = useState(isTransporter);
  const [saving, setSaving] = useState(false);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [displayName, setDisplayName] = useState('');
  const [vehicleDescription, setVehicleDescription] = useState('');
  const [capacityKg, setCapacityKg] = useState('');
  const [routesServed, setRoutesServed] = useState<string[]>([]);

  const hydrateForm = useCallback((next: TransporterProfileDto) => {
    setProfile(next);
    setDisplayName(next.displayName);
    setVehicleDescription(next.vehicleDescription);
    setCapacityKg(String(next.capacityKg));
    setRoutesServed([...next.routesServed]);
  }, []);

  const loadProfile = useCallback(async () => {
    if (!isTransporter) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setFormError(null);
    try {
      const { data, error } = await getApiProfileMe();
      if (error) {
        if (extractErrorCode(error) === 'ProfileNotFound') {
          setProfile(null);
          return;
        }
        setFormError(messageForApiError(error, 'Unable to load your profile.'));
        return;
      }

      if (data) {
        hydrateForm(data);
      }
    } catch {
      setFormError('Unable to load your profile. Check your connection.');
    } finally {
      setLoading(false);
    }
  }, [hydrateForm, isTransporter]);

  useFocusEffect(
    useCallback(() => {
      void loadProfile();
    }, [loadProfile]),
  );

  useEffect(() => {
    if (userId) {
      void useVerificationApplicationStore.getState().hydrateForUser(userId);
    }
  }, [userId]);

  function toggleRoute(route: string) {
    setRoutesServed((current) =>
      current.includes(route) ? current.filter((item) => item !== route) : [...current, route],
    );
  }

  async function handleSave() {
    setFormError(null);
    const trimmedName = displayName.trim();
    const trimmedVehicle = vehicleDescription.trim();
    const parsedCapacity = Number(capacityKg.trim());

    if (!trimmedName) {
      setFormError('Enter a display name.');
      return;
    }
    if (routesServed.length === 0) {
      setFormError('Select at least one route you serve.');
      return;
    }
    if (!trimmedVehicle) {
      setFormError('Describe your vehicle.');
      return;
    }
    if (!Number.isFinite(parsedCapacity) || parsedCapacity <= 0) {
      setFormError('Enter a capacity greater than 0 kg.');
      return;
    }

    const body = {
      displayName: trimmedName,
      routesServed,
      vehicleDescription: trimmedVehicle,
      capacityKg: parsedCapacity,
    };

    setSaving(true);
    try {
      if (profile) {
        const { data, error } = await putApiProfile({ body });
        if (error || !data) {
          setFormError(messageForApiError(error, 'Unable to update your profile.'));
          return;
        }
        hydrateForm(data);
        return;
      }

      const { data, error } = await postApiProfile({
        body: { ...body, profilePhotoUrl: null },
      });
      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to create your profile.'));
        return;
      }
      hydrateForm(data);
    } catch {
      setFormError('Unable to save your profile. Check your connection.');
    } finally {
      setSaving(false);
    }
  }

  async function handlePickPhoto() {
    if (!profile) {
      setFormError('Save your profile before uploading a photo.');
      return;
    }

    setFormError(null);
    const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
    if (!permission.granted) {
      setFormError('Photo library permission is required to upload a profile photo.');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.85,
    });

    if (result.canceled || !result.assets[0]) {
      return;
    }

    const asset = result.assets[0];
    const contentType = asset.mimeType ?? 'image/jpeg';
    if (!PROFILE_PHOTO_CONTENT_TYPES.includes(contentType as (typeof PROFILE_PHOTO_CONTENT_TYPES)[number])) {
      setFormError('Use a JPEG, PNG, or WebP image.');
      return;
    }

    const fileName =
      asset.fileName ?? `profile.${extensionForContentType(contentType)}`;

    setUploadingPhoto(true);
    try {
      const { data, error } = await postApiProfilePhoto({
        body: { fileName, contentType },
      });

      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to start photo upload.'));
        return;
      }

      const blob = await (await fetch(asset.uri)).blob();
      const uploadResponse = await fetch(data.uploadUrl, {
        method: 'PUT',
        headers: { 'Content-Type': contentType },
        body: blob,
      });

      if (!uploadResponse.ok) {
        setFormError('Photo upload to storage failed. Try again.');
        return;
      }

      setProfile((current) =>
        current
          ? { ...current, profilePhotoUrl: data.profilePhotoUrl, updatedAtUtc: new Date().toISOString() }
          : current,
      );
    } catch {
      setFormError('Unable to upload photo. Check your connection.');
    } finally {
      setUploadingPhoto(false);
    }
  }

  async function handleSignOut() {
    await clearVerification();
    await clearRequests();
    await clearSession();
  }

  if (!isTransporter) {
    return (
      <ThemedView style={styles.container}>
        <SafeAreaView style={styles.safeArea}>
          <ThemedView style={styles.header}>
            <ThemedText type="subtitle">Account</ThemedText>
            <ThemedText type="default" themeColor="textSecondary">
              {phoneNumber ?? 'Unknown phone'} · {role ?? 'Unknown role'}
            </ThemedText>
            <ThemedText type="small" themeColor="textSecondary">
              Transporter profiles are available when you sign in as a Transporter.
            </ThemedText>
          </ThemedView>
          <Button label="Sign out" variant="secondary" onPress={() => void handleSignOut()} />
        </SafeAreaView>
      </ThemedView>
    );
  }

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
        <KeyboardAvoidingView
          style={styles.flex}
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
            <ThemedView style={styles.header}>
              <ThemedText type="subtitle">
                {profile ? 'Your profile' : 'Create profile'}
              </ThemedText>
              <ThemedText type="default" themeColor="textSecondary">
                Tell senders about your routes, vehicle, and capacity.
              </ThemedText>
            </ThemedView>

            <ThemedView style={styles.photoSection}>
              {profile?.profilePhotoUrl ? (
                <Image
                  source={{ uri: profile.profilePhotoUrl }}
                  style={styles.avatar}
                  accessibilityLabel="Profile photo"
                />
              ) : (
                <ThemedView type="backgroundSelected" style={styles.avatarFallback}>
                  <ThemedText type="smallBold">
                    {(displayName || 'T').slice(0, 1).toUpperCase()}
                  </ThemedText>
                </ThemedView>
              )}

              <ThemedView style={styles.photoMeta}>
                {profile?.isVerified ? (
                  <ThemedText type="smallBold" style={styles.badge}>
                    Verified oMalayisha
                  </ThemedText>
                ) : (
                  <ThemedText type="small" themeColor="textSecondary">
                    Not verified yet
                  </ThemedText>
                )}
                {profile ? (
                  <ThemedText type="small" themeColor="textSecondary">
                    ★ {formatRating(profile.averageRating)}
                  </ThemedText>
                ) : null}
                <Button
                  label={uploadingPhoto ? 'Uploading…' : 'Upload photo'}
                  variant="secondary"
                  loading={uploadingPhoto}
                  disabled={!profile}
                  onPress={() => void handlePickPhoto()}
                />
              </ThemedView>
            </ThemedView>

            <TextField
              label="Display name"
              value={displayName}
              onChangeText={setDisplayName}
              placeholder="e.g. Tendai Express"
            />

            <ThemedView style={styles.block}>
              <ThemedText type="smallBold">Routes served</ThemedText>
              <View style={styles.chipRow}>
                {PROFILE_ROUTE_OPTIONS.map((route) => (
                  <CityChip
                    key={route}
                    label={route}
                    selected={routesServed.includes(route)}
                    onPress={() => toggleRoute(route)}
                  />
                ))}
              </View>
            </ThemedView>

            <TextField
              label="Vehicle description"
              value={vehicleDescription}
              onChangeText={setVehicleDescription}
              placeholder="e.g. Toyota Quantum, reliable for parcels"
              multiline
              style={styles.multiline}
            />

            <TextField
              label="Capacity (kg)"
              value={capacityKg}
              onChangeText={setCapacityKg}
              keyboardType="decimal-pad"
              placeholder="e.g. 500"
            />

            <ErrorBanner message={formError} />

            <Button
              label={profile ? 'Save changes' : 'Create profile'}
              loading={saving}
              onPress={() => void handleSave()}
            />

            {profile ? (
              <Button
                label="Verification"
                variant="secondary"
                onPress={() => router.push('/profile/verification')}
              />
            ) : null}

            <Button label="Sign out" variant="secondary" onPress={() => void handleSignOut()} />
          </ScrollView>
        </KeyboardAvoidingView>
      </SafeAreaView>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  flex: {
    flex: 1,
  },
  safeArea: {
    flex: 1,
    maxWidth: MaxContentWidth,
    width: '100%',
    alignSelf: 'center',
    paddingHorizontal: Spacing.four,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  content: {
    gap: Spacing.three,
    paddingTop: Spacing.three,
    paddingBottom: BottomTabInset + Spacing.five,
  },
  header: {
    gap: Spacing.two,
  },
  photoSection: {
    flexDirection: 'row',
    gap: Spacing.three,
    alignItems: 'center',
  },
  photoMeta: {
    flex: 1,
    gap: Spacing.two,
  },
  avatar: {
    width: 88,
    height: 88,
    borderRadius: 44,
  },
  avatarFallback: {
    width: 88,
    height: 88,
    borderRadius: 44,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badge: {
    color: PRIMARY_COLOR,
  },
  block: {
    gap: Spacing.two,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.two,
  },
  multiline: {
    minHeight: 88,
    textAlignVertical: 'top',
    paddingTop: Spacing.three,
  },
});
