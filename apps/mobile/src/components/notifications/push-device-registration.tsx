import { useEffect } from 'react';

import { AuthRole } from '@/constants/auth';
import { registerPushDeviceTokenWithApi } from '@/lib/push-notifications';
import { useAuthStore } from '@/stores/auth-store';

/**
 * Registers the FCM/APNs device token after login for Sender and Transporter roles.
 * Renders nothing — mount once under the authenticated app tree.
 */
export function PushDeviceRegistration() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const role = useAuthStore((state) => state.role);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    if (role !== AuthRole.Sender && role !== AuthRole.Transporter) {
      return;
    }

    void registerPushDeviceTokenWithApi();
  }, [isAuthenticated, role]);

  return null;
}
