import { useEffect, useMemo, useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet } from 'react-native';
import { router, useLocalSearchParams, useNavigation } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';

import { postApiRequests, putApiRequestsById } from '@/api';
import { CityChip } from '@/components/trips/trip-result-card';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Button, ErrorBanner, TextField } from '@/components/ui/form-controls';
import {
  DESTINATION_CITIES,
  ORIGIN_CITIES,
  type DestinationCity,
  type OriginCity,
} from '@/constants/corridors';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import { messageForApiError } from '@/lib/api-errors';
import { formatDateOnly } from '@/lib/format';
import { useDeliveryRequestsStore } from '@/stores/delivery-requests-store';

const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

function toRequiredDateUtc(dateOnly: string): string {
  return `${dateOnly}T12:00:00.000Z`;
}

function isFutureDateOnly(dateOnly: string): boolean {
  const today = new Date();
  const todayUtc = Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate());
  const [year, month, day] = dateOnly.split('-').map(Number);
  const selectedUtc = Date.UTC(year, month - 1, day);
  return selectedUtc > todayUtc;
}

type FormState = {
  originCity: OriginCity;
  destinationCity: DestinationCity;
  requiredDate: string;
  weightKg: string;
  sizeDescription: string;
  goodsDescription: string;
};

export default function CreateRequestScreen() {
  const navigation = useNavigation();
  const params = useLocalSearchParams<{ id?: string }>();
  const requestId = typeof params.id === 'string' ? params.id : undefined;
  const isEdit = Boolean(requestId);

  const getById = useDeliveryRequestsStore((state) => state.getById);
  const upsert = useDeliveryRequestsStore((state) => state.upsert);
  const existing = requestId ? getById(requestId) : undefined;

  useEffect(() => {
    navigation.setOptions({
      title: isEdit ? 'Edit request' : 'New request',
    });
  }, [isEdit, navigation]);

  const initial = useMemo<FormState>(() => {
    if (existing) {
      const origin = ORIGIN_CITIES.includes(existing.originCity as OriginCity)
        ? (existing.originCity as OriginCity)
        : ORIGIN_CITIES[0];
      const destination = DESTINATION_CITIES.includes(existing.destinationCity as DestinationCity)
        ? (existing.destinationCity as DestinationCity)
        : DESTINATION_CITIES[0];

      return {
        originCity: origin,
        destinationCity: destination,
        requiredDate: formatDateOnly(existing.requiredDateUtc),
        weightKg: String(existing.weightKg),
        sizeDescription: existing.sizeDescription,
        goodsDescription: existing.goodsDescription,
      };
    }

    return {
      originCity: ORIGIN_CITIES[0],
      destinationCity: DESTINATION_CITIES[0],
      requiredDate: '',
      weightKg: '',
      sizeDescription: '',
      goodsDescription: '',
    };
  }, [existing]);

  const [originCity, setOriginCity] = useState(initial.originCity);
  const [destinationCity, setDestinationCity] = useState(initial.destinationCity);
  const [requiredDate, setRequiredDate] = useState(initial.requiredDate);
  const [weightKg, setWeightKg] = useState(initial.weightKg);
  const [sizeDescription, setSizeDescription] = useState(initial.sizeDescription);
  const [goodsDescription, setGoodsDescription] = useState(initial.goodsDescription);

  const [dateError, setDateError] = useState<string | null>(null);
  const [weightError, setWeightError] = useState<string | null>(null);
  const [sizeError, setSizeError] = useState<string | null>(null);
  const [goodsError, setGoodsError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setOriginCity(initial.originCity);
    setDestinationCity(initial.destinationCity);
    setRequiredDate(initial.requiredDate);
    setWeightKg(initial.weightKg);
    setSizeDescription(initial.sizeDescription);
    setGoodsDescription(initial.goodsDescription);
  }, [initial]);

  if (isEdit && !existing) {
    return (
      <ThemedView style={styles.container}>
        <SafeAreaView style={styles.safeArea}>
          <ThemedText type="subtitle">Request not found</ThemedText>
          <ThemedText type="default" themeColor="textSecondary">
            This delivery request is not available on this device.
          </ThemedText>
          <Button label="Back to list" variant="secondary" onPress={() => router.back()} />
        </SafeAreaView>
      </ThemedView>
    );
  }

  async function handleSubmit() {
    setFormError(null);
    setDateError(null);
    setWeightError(null);
    setSizeError(null);
    setGoodsError(null);

    const trimmedDate = requiredDate.trim();
    const trimmedWeight = weightKg.trim();
    const trimmedSize = sizeDescription.trim();
    const trimmedGoods = goodsDescription.trim();

    let valid = true;

    if (!DATE_PATTERN.test(trimmedDate)) {
      setDateError('Use yyyy-MM-dd, e.g. 2026-08-15.');
      valid = false;
    } else if (!isFutureDateOnly(trimmedDate)) {
      setDateError('Required date must be after today.');
      valid = false;
    }

    const parsedWeight = Number(trimmedWeight);
    if (!Number.isFinite(parsedWeight) || parsedWeight <= 0) {
      setWeightError('Enter a weight greater than 0 kg.');
      valid = false;
    }

    if (!trimmedSize) {
      setSizeError('Describe the package size.');
      valid = false;
    } else if (trimmedSize.length > 200) {
      setSizeError('Size description must be 200 characters or fewer.');
      valid = false;
    }

    if (!trimmedGoods) {
      setGoodsError('Describe the goods to send.');
      valid = false;
    } else if (trimmedGoods.length > 2000) {
      setGoodsError('Goods description must be 2000 characters or fewer.');
      valid = false;
    }

    if (!valid) {
      return;
    }

    const body = {
      originCity,
      destinationCity,
      requiredDateUtc: toRequiredDateUtc(trimmedDate),
      weightKg: parsedWeight,
      sizeDescription: trimmedSize,
      goodsDescription: trimmedGoods,
    };

    setLoading(true);
    try {
      if (isEdit && requestId) {
        const { data, error } = await putApiRequestsById({
          path: { id: requestId },
          body,
        });

        if (error || !data) {
          setFormError(messageForApiError(error, 'Unable to update this request.'));
          return;
        }

        await upsert(data);
        router.back();
        return;
      }

      const { data, error } = await postApiRequests({ body });

      if (error || !data) {
        setFormError(messageForApiError(error, 'Unable to create this request.'));
        return;
      }

      await upsert(data);
      router.back();
    } catch {
      setFormError('Unable to save this request. Check your connection.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['bottom']}>
        <KeyboardAvoidingView
          style={styles.flex}
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
          <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
            <ThemedText type="default" themeColor="textSecondary">
              {isEdit
                ? 'Update the details transporters will see.'
                : 'Post what you need to send so transporters can find you.'}
            </ThemedText>

            <ThemedView style={styles.block}>
              <ThemedText type="smallBold">From</ThemedText>
              <ThemedView style={styles.chipRow}>
                {ORIGIN_CITIES.map((city) => (
                  <CityChip
                    key={city}
                    label={city}
                    selected={originCity === city}
                    onPress={() => setOriginCity(city)}
                  />
                ))}
              </ThemedView>
            </ThemedView>

            <ThemedView style={styles.block}>
              <ThemedText type="smallBold">To</ThemedText>
              <ThemedView style={styles.chipRow}>
                {DESTINATION_CITIES.map((city) => (
                  <CityChip
                    key={city}
                    label={city}
                    selected={destinationCity === city}
                    onPress={() => setDestinationCity(city)}
                  />
                ))}
              </ThemedView>
            </ThemedView>

            <TextField
              label="Required date"
              value={requiredDate}
              onChangeText={setRequiredDate}
              placeholder="yyyy-MM-dd"
              autoCapitalize="none"
              autoCorrect={false}
              error={dateError}
            />

            <TextField
              label="Weight (kg)"
              value={weightKg}
              onChangeText={setWeightKg}
              placeholder="e.g. 25"
              keyboardType="decimal-pad"
              error={weightError}
            />

            <TextField
              label="Size description"
              value={sizeDescription}
              onChangeText={setSizeDescription}
              placeholder="e.g. Medium suitcase"
              error={sizeError}
            />

            <TextField
              label="Goods description"
              value={goodsDescription}
              onChangeText={setGoodsDescription}
              placeholder="What are you sending?"
              multiline
              style={styles.multiline}
              error={goodsError}
            />

            <ErrorBanner message={formError} />

            <Button
              label={isEdit ? 'Save changes' : 'Create request'}
              loading={loading}
              onPress={() => void handleSubmit()}
            />
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
  content: {
    gap: Spacing.three,
    paddingTop: Spacing.three,
    paddingBottom: Spacing.six,
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
    minHeight: 96,
    textAlignVertical: 'top',
    paddingTop: Spacing.three,
  },
});
