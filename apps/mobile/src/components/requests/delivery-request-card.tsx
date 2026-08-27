import { Alert, Pressable, StyleSheet, View } from 'react-native';

import type { DeliveryRequestDto } from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import {
  formatDeliveryRequestStatus,
  isEditableDeliveryRequestStatus,
} from '@/constants/delivery-requests';
import { Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';
import { formatDateOnly, formatKg } from '@/lib/format';

type DeliveryRequestCardProps = {
  request: DeliveryRequestDto;
  onEdit: () => void;
  onCancel: () => void;
  cancelling?: boolean;
};

export function DeliveryRequestCard({
  request,
  onEdit,
  onCancel,
  cancelling = false,
}: DeliveryRequestCardProps) {
  const theme = useTheme();
  const editable = isEditableDeliveryRequestStatus(request.status);

  function confirmCancel() {
    Alert.alert(
      'Cancel request?',
      'Transporters will no longer see this delivery request.',
      [
        { text: 'Keep', style: 'cancel' },
        {
          text: 'Cancel request',
          style: 'destructive',
          onPress: onCancel,
        },
      ],
    );
  }

  return (
    <ThemedView
      type="backgroundElement"
      style={[styles.card, { borderColor: theme.backgroundSelected }]}>
      <ThemedView style={styles.header}>
        <ThemedText type="smallBold">
          {request.originCity} → {request.destinationCity}
        </ThemedText>
        <ThemedText type="small" themeColor="textSecondary">
          {formatDeliveryRequestStatus(request.status)}
        </ThemedText>
      </ThemedView>

      <ThemedText type="small" themeColor="textSecondary">
        Needed {formatDateOnly(request.requiredDateUtc)} · {formatKg(request.weightKg)}
      </ThemedText>
      <ThemedText type="small">{request.goodsDescription}</ThemedText>
      <ThemedText type="small" themeColor="textSecondary">
        Size: {request.sizeDescription}
      </ThemedText>

      {editable ? (
        <View style={styles.actions}>
          <Pressable accessibilityRole="button" onPress={onEdit} disabled={cancelling}>
            <ThemedText type="linkPrimary">Edit</ThemedText>
          </Pressable>
          <Pressable accessibilityRole="button" onPress={confirmCancel} disabled={cancelling}>
            <ThemedText type="small" style={styles.cancelLabel}>
              {cancelling ? 'Cancelling…' : 'Cancel'}
            </ThemedText>
          </Pressable>
        </View>
      ) : null}
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: Spacing.two,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    gap: Spacing.two,
  },
  actions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: Spacing.one,
  },
  cancelLabel: {
    color: '#D14343',
  },
});
