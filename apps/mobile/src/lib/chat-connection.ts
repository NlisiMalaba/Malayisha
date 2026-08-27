import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';

import { getApiBaseUrl } from '@/api/base-url';
import type { ChatMessageDto } from '@/api';
import { useAuthStore } from '@/stores/auth-store';

export const CHAT_RECEIVE_METHOD = 'ReceiveMessage';
export const CHAT_SEND_METHOD = 'SendMessage';
export const CHAT_MAX_MESSAGE_LENGTH = 2000;

export type ChatConnectionStatus = 'connecting' | 'connected' | 'disconnected' | 'reconnecting';

export function mapHubState(state: HubConnectionState): ChatConnectionStatus {
  switch (state) {
    case HubConnectionState.Connected:
      return 'connected';
    case HubConnectionState.Connecting:
      return 'connecting';
    case HubConnectionState.Reconnecting:
      return 'reconnecting';
    default:
      return 'disconnected';
  }
}

export function createChatConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${getApiBaseUrl()}/hubs/chat`, {
      accessTokenFactory: async () => useAuthStore.getState().accessToken ?? '',
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();
}

export function isChatMessageDto(value: unknown): value is ChatMessageDto {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const message = value as Record<string, unknown>;
  return (
    typeof message.id === 'string' &&
    typeof message.bookingId === 'string' &&
    typeof message.senderUserId === 'string' &&
    typeof message.text === 'string' &&
    typeof message.sentAtUtc === 'string'
  );
}
