import AppTabs from '@/components/app-tabs';
import { PushDeviceRegistration } from '@/components/notifications/push-device-registration';

export default function AppLayout() {
  return (
    <>
      <PushDeviceRegistration />
      <AppTabs />
    </>
  );
}
