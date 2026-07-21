import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  StyleSheet,
  TextInput,
  View,
} from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';

import { getApiBookingsByIdMessages, type ChatMessageDto } from '@/api';
import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PRIMARY_COLOR } from '@/components/ui/form-controls';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import { useTheme } from '@/hooks/use-theme';
import { messageForApiError } from '@/lib/api-errors';
import {
  CHAT_MAX_MESSAGE_LENGTH,
  CHAT_RECEIVE_METHOD,
  CHAT_SEND_METHOD,
  createChatConnection,
  isChatMessageDto,
  mapHubState,
  type ChatConnectionStatus,
} from '@/lib/chat-connection';
import { useAuthStore } from '@/stores/auth-store';

function sortMessages(messages: ChatMessageDto[]): ChatMessageDto[] {
  return [...messages].sort(
    (a, b) => new Date(a.sentAtUtc).getTime() - new Date(b.sentAtUtc).getTime(),
  );
}

function mergeMessage(messages: ChatMessageDto[], incoming: ChatMessageDto): ChatMessageDto[] {
  if (messages.some((message) => message.id === incoming.id)) {
    return messages;
  }
  return sortMessages([...messages, incoming]);
}

function formatMessageTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function statusLabel(status: ChatConnectionStatus): string {
  switch (status) {
    case 'connected':
      return 'Connected';
    case 'connecting':
      return 'Connecting…';
    case 'reconnecting':
      return 'Reconnecting…';
    default:
      return 'Offline';
  }
}

export default function ChatScreen() {
  const theme = useTheme();
  const params = useLocalSearchParams<{ id?: string }>();
  const bookingId = typeof params.id === 'string' ? params.id : undefined;
  const userId = useAuthStore((state) => state.userId);

  const connectionRef = useRef<HubConnection | null>(null);
  const listRef = useRef<FlatList<ChatMessageDto>>(null);

  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [draft, setDraft] = useState('');
  const [historyLoading, setHistoryLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<ChatConnectionStatus>('connecting');
  const [error, setError] = useState<string | null>(null);

  const isOffline = connectionStatus !== 'connected';
  const canSend = useMemo(() => {
    const trimmed = draft.trim();
    return (
      Boolean(bookingId) &&
      trimmed.length > 0 &&
      trimmed.length <= CHAT_MAX_MESSAGE_LENGTH &&
      !sending &&
      connectionStatus === 'connected'
    );
  }, [bookingId, connectionStatus, draft, sending]);

  const loadHistory = useCallback(async () => {
    if (!bookingId) {
      setError('Missing booking id.');
      setHistoryLoading(false);
      return;
    }

    setHistoryLoading(true);
    try {
      const { data, error: historyError } = await getApiBookingsByIdMessages({
        path: { id: bookingId },
      });

      if (historyError || !data) {
        setError(messageForApiError(historyError, 'Unable to load chat history.'));
        return;
      }

      setMessages(sortMessages(data.messages ?? []));
      setError(null);
    } catch {
      setError('Unable to load chat history. Check your connection.');
    } finally {
      setHistoryLoading(false);
    }
  }, [bookingId]);

  useEffect(() => {
    void loadHistory();
  }, [loadHistory]);

  useEffect(() => {
    if (!bookingId) {
      return;
    }

    let disposed = false;
    const connection = createChatConnection();
    connectionRef.current = connection;

    const syncStatus = () => {
      if (!disposed) {
        setConnectionStatus(mapHubState(connection.state));
      }
    };

    connection.onreconnecting(() => {
      if (!disposed) {
        setConnectionStatus('reconnecting');
      }
    });
    connection.onreconnected(() => {
      if (!disposed) {
        setConnectionStatus('connected');
      }
    });
    connection.onclose(() => {
      if (!disposed) {
        setConnectionStatus('disconnected');
      }
    });

    connection.on(CHAT_RECEIVE_METHOD, (payload: unknown) => {
      if (!isChatMessageDto(payload) || payload.bookingId !== bookingId) {
        return;
      }

      setMessages((current) => mergeMessage(current, payload));
    });

    setConnectionStatus('connecting');
    void connection
      .start()
      .then(() => {
        if (!disposed) {
          setConnectionStatus(mapHubState(connection.state));
        }
      })
      .catch(() => {
        if (!disposed) {
          setConnectionStatus('disconnected');
          setError((current) => current ?? 'Unable to connect to chat. You are offline.');
        }
      });

    syncStatus();

    return () => {
      disposed = true;
      connection.off(CHAT_RECEIVE_METHOD);
      connectionRef.current = null;
      void connection.stop();
    };
  }, [bookingId]);

  useEffect(() => {
    if (messages.length === 0) {
      return;
    }
    const handle = requestAnimationFrame(() => {
      listRef.current?.scrollToEnd({ animated: true });
    });
    return () => cancelAnimationFrame(handle);
  }, [messages.length]);

  async function handleSend() {
    const text = draft.trim();
    if (!bookingId || !canSend || !text) {
      return;
    }

    if (text.length > CHAT_MAX_MESSAGE_LENGTH) {
      setError(`Messages must be ${CHAT_MAX_MESSAGE_LENGTH} characters or fewer.`);
      return;
    }

    const connection = connectionRef.current;
    if (!connection || connection.state !== HubConnectionState.Connected) {
      setError('You are offline. Reconnect before sending.');
      return;
    }

    setSending(true);
    setError(null);
    try {
      await connection.invoke(CHAT_SEND_METHOD, bookingId, text);
      setDraft('');
    } catch (sendError) {
      const message =
        sendError instanceof Error && sendError.message
          ? sendError.message
          : 'Unable to send message.';
      setError(message.includes('MessageTooLong')
        ? `Messages must be ${CHAT_MAX_MESSAGE_LENGTH} characters or fewer.`
        : message);
    } finally {
      setSending(false);
    }
  }

  return (
    <ThemedView style={styles.container}>
      <SafeAreaView style={styles.safeArea} edges={['bottom']}>
        <ThemedView
          type="backgroundElement"
          style={[
            styles.statusBanner,
            isOffline ? styles.statusOffline : styles.statusOnline,
          ]}>
          <ThemedText type="smallBold" style={isOffline ? styles.statusOfflineText : undefined}>
            {statusLabel(connectionStatus)}
          </ThemedText>
          {isOffline ? (
            <ThemedText type="small" style={styles.statusOfflineText}>
              Messages will send when you are back online.
            </ThemedText>
          ) : null}
        </ThemedView>

        <KeyboardAvoidingView
          style={styles.flex}
          behavior={Platform.OS === 'ios' ? 'padding' : undefined}
          keyboardVerticalOffset={88}>
          {historyLoading ? (
            <View style={styles.centered}>
              <ActivityIndicator color={PRIMARY_COLOR} />
            </View>
          ) : (
            <FlatList
              ref={listRef}
              data={messages}
              keyExtractor={(item) => item.id}
              contentContainerStyle={styles.listContent}
              onContentSizeChange={() => listRef.current?.scrollToEnd({ animated: false })}
              ListEmptyComponent={
                <ThemedText type="small" themeColor="textSecondary" style={styles.empty}>
                  No messages yet. Say hello to start the conversation.
                </ThemedText>
              }
              renderItem={({ item }) => {
                const mine = item.senderUserId === userId;
                return (
                  <View style={[styles.bubbleRow, mine ? styles.bubbleRowMine : styles.bubbleRowTheirs]}>
                    <ThemedView
                      style={[
                        styles.bubble,
                        {
                          backgroundColor: mine ? PRIMARY_COLOR : theme.backgroundElement,
                        },
                      ]}>
                      <ThemedText
                        type="small"
                        style={mine ? styles.bubbleTextMine : undefined}>
                        {item.text}
                      </ThemedText>
                      <ThemedText
                        type="small"
                        style={[
                          styles.bubbleTime,
                          mine ? styles.bubbleTextMine : { color: theme.textSecondary },
                        ]}>
                        {formatMessageTime(item.sentAtUtc)}
                      </ThemedText>
                    </ThemedView>
                  </View>
                );
              }}
            />
          )}

          {error ? (
            <ThemedView type="backgroundElement" style={styles.errorBanner}>
              <ThemedText type="small" style={styles.errorText}>
                {error}
              </ThemedText>
            </ThemedView>
          ) : null}

          <ThemedView style={styles.composer}>
            <TextInput
              value={draft}
              onChangeText={setDraft}
              placeholder="Type a message"
              placeholderTextColor={theme.textSecondary}
              multiline
              maxLength={CHAT_MAX_MESSAGE_LENGTH}
              editable={!sending}
              style={[
                styles.input,
                {
                  color: theme.text,
                  backgroundColor: theme.backgroundElement,
                  borderColor: theme.backgroundSelected,
                },
              ]}
            />
            <Pressable
              accessibilityRole="button"
              disabled={!canSend}
              onPress={() => void handleSend()}
              style={[styles.sendButton, !canSend && styles.sendDisabled]}>
              {sending ? (
                <ActivityIndicator color="#ffffff" />
              ) : (
                <ThemedText type="smallBold" style={styles.sendLabel}>
                  Send
                </ThemedText>
              )}
            </Pressable>
          </ThemedView>
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
  },
  statusBanner: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    gap: Spacing.half,
  },
  statusOnline: {
    backgroundColor: '#E7F6EC',
  },
  statusOffline: {
    backgroundColor: '#FDECEC',
  },
  statusOfflineText: {
    color: '#D14343',
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  listContent: {
    flexGrow: 1,
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    gap: Spacing.two,
  },
  empty: {
    textAlign: 'center',
    marginTop: Spacing.five,
  },
  bubbleRow: {
    flexDirection: 'row',
  },
  bubbleRowMine: {
    justifyContent: 'flex-end',
  },
  bubbleRowTheirs: {
    justifyContent: 'flex-start',
  },
  bubble: {
    maxWidth: '80%',
    borderRadius: Spacing.three,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    gap: Spacing.half,
  },
  bubbleTextMine: {
    color: '#ffffff',
  },
  bubbleTime: {
    fontSize: 11,
    alignSelf: 'flex-end',
  },
  errorBanner: {
    marginHorizontal: Spacing.four,
    marginBottom: Spacing.two,
    borderRadius: Spacing.two,
    padding: Spacing.three,
  },
  errorText: {
    color: '#D14343',
  },
  composer: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: Spacing.two,
    paddingHorizontal: Spacing.four,
    paddingBottom: Spacing.three,
    paddingTop: Spacing.two,
  },
  input: {
    flex: 1,
    minHeight: 44,
    maxHeight: 120,
    borderWidth: 1,
    borderRadius: Spacing.two,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    fontSize: 16,
  },
  sendButton: {
    minHeight: 44,
    minWidth: 72,
    borderRadius: Spacing.two,
    backgroundColor: PRIMARY_COLOR,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: Spacing.three,
  },
  sendDisabled: {
    opacity: 0.5,
  },
  sendLabel: {
    color: '#ffffff',
  },
});
