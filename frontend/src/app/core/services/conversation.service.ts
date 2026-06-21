import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { buildApiUrl, buildBackendUrl } from '../utils/api-url';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

export type ConversationType = 'Direct' | 'Group';

export interface ParticipantDto {
  id: number;
  userId: number;
  userName: string;
  role: string;
  joinedAt: string;
  lastReadAt: string | null;
}

export interface MessageDto {
  id: number;
  conversationId: number;
  senderId: number;
  senderName: string;
  content: string;
  attachmentUrl: string | null;
  attachmentType: string | null;
  voiceText?: string;
  sentAt: string;
  isEdited?: boolean;
}

export interface ConversationDto {
  id: number;
  title: string | null;
  type: ConversationType;
  lastMessageAt: string;
  participants: ParticipantDto[];
  lastMessage: MessageDto | null;
}

export interface ApiResponse<T> {
  isSuccess: boolean;
  message: string;
  statusCode: number;
  data: T;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface BlockStatusDto {
  isBlocked: boolean;
  blockedByMe: boolean;
  blockedByOther: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConversationService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private baseUrl = buildApiUrl('conversation');
  private hubUrl = buildBackendUrl('/hubs/chat');

  private hubConnection: signalR.HubConnection | null = null;
  isConnected = signal(false);
  receivedMessage$ = new Subject<MessageDto>();
  updatedMessage$ = new Subject<MessageDto>();
  deletedMessage$ = new Subject<{ conversationId: number; messageId: number }>();
  userTypingIn = signal<{ conversationId: number; userId: number } | null>(null);
  userStoppedTypingIn = signal<{ conversationId: number; userId: number } | null>(null);
  readReceipt = signal<{ conversationId: number; userId: number } | null>(null);

  startConnection(): void {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;
    const token = this.auth.getToken();
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => token ?? '' })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('MessageReceived', (msg: MessageDto) => this.receivedMessage$.next(msg));
    this.hubConnection.on('MessageUpdated', (msg: MessageDto) => this.updatedMessage$.next(msg));
    this.hubConnection.on('MessageDeleted', (conversationId: number, messageId: number) => this.deletedMessage$.next({ conversationId, messageId }));
    this.hubConnection.on('UserTyping', (conversationId: number, userId: number) => {
      this.userTypingIn.set({ conversationId, userId });
      setTimeout(() => {
        if (this.userTypingIn()?.userId === userId && this.userTypingIn()?.conversationId === conversationId) {
          this.userTypingIn.set(null);
        }
      }, 3000);
    });
    this.hubConnection.on('UserStoppedTyping', (conversationId: number, userId: number) => {
      if (this.userTypingIn()?.conversationId === conversationId && this.userTypingIn()?.userId === userId) {
        this.userTypingIn.set(null);
      }
      this.userStoppedTypingIn.set({ conversationId, userId });
    });
    this.hubConnection.on('ReadReceipt', (conversationId: number, userId: number) => this.readReceipt.set({ conversationId, userId }));

    this.hubConnection.onreconnecting(() => this.isConnected.set(false));
    this.hubConnection.onreconnected(() => this.isConnected.set(true));
    this.hubConnection.onclose(() => this.isConnected.set(false));

    this.hubConnection.start().then(() => this.isConnected.set(true)).catch(() => {});
  }

  stopConnection(): void {
    this.hubConnection?.stop().then(() => {
      this.isConnected.set(false);
      this.hubConnection = null;
    }).catch(() => {});
  }

  async joinConversation(conversationId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinConversation', conversationId).catch(() => {});
    }
  }

  async leaveConversation(conversationId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveConversation', conversationId).catch(() => {});
    }
  }

  async sendMessageSignalR(conversationId: number, content: string): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('SendMessage', conversationId, content);
    } else {
      throw new Error('SignalR not connected');
    }
  }

  async markAsReadSignalR(conversationId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('MarkAsRead', conversationId).catch(() => {});
    }
  }

  async sendTypingSignalR(conversationId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('UserTyping', conversationId).catch(() => {});
    }
  }

  async sendStoppedTypingSignalR(conversationId: number): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      await this.hubConnection.invoke('UserStoppedTyping', conversationId).catch(() => {});
    }
  }

  getMyConversations(): Observable<ApiResponse<ConversationDto[]>> {
    return this.http.get<ApiResponse<ConversationDto[]>>(`${this.baseUrl}/my`);
  }

  getConversationById(id: number): Observable<ApiResponse<ConversationDto>> {
    return this.http.get<ApiResponse<ConversationDto>>(`${this.baseUrl}/${id}`);
  }

  getMessages(conversationId: number, page = 1, pageSize = 50): Observable<ApiResponse<PagedResult<MessageDto>>> {
    return this.http.get<ApiResponse<PagedResult<MessageDto>>>(`${this.baseUrl}/${conversationId}/messages`, {
      params: { page, pageSize: pageSize.toString() }
    });
  }

  updateMessage(conversationId: number, messageId: number, content: string): Observable<ApiResponse<MessageDto>> {
    return this.http.put<ApiResponse<MessageDto>>(`${this.baseUrl}/${conversationId}/messages/${messageId}`, { content });
  }

  deleteMessage(conversationId: number, messageId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl}/${conversationId}/messages/${messageId}`);
  }

  transcribeMessage(conversationId: number, messageId: number, voiceText: string): Observable<ApiResponse<MessageDto>> {
    return this.http.put<ApiResponse<MessageDto>>(`${this.baseUrl}/${conversationId}/messages/${messageId}/transcribe`, { voiceText });
  }

  blockUser(conversationId: number, blockedUserId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/${conversationId}/block/${blockedUserId}`, {});
  }

  unblockUser(conversationId: number, blockedUserId: number): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.baseUrl}/${conversationId}/block/${blockedUserId}`);
  }

  isUserBlocked(conversationId: number, otherUserId: number): Observable<ApiResponse<BlockStatusDto>> {
    return this.http.get<ApiResponse<BlockStatusDto>>(`${this.baseUrl}/${conversationId}/block/${otherUserId}`);
  }

  uploadFile(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${buildApiUrl('upload')}/chat`, formData);
  }

  sendMessageRest(conversationId: number, content: string, attachmentUrl?: string, attachmentType?: string, voiceText?: string): Observable<ApiResponse<MessageDto>> {
    return this.http.post<ApiResponse<MessageDto>>(`${this.baseUrl}/${conversationId}/messages`, { content, attachmentUrl, attachmentType, voiceText });
  }

  markAsReadRest(conversationId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/${conversationId}/read`, {});
  }

  getParticipants(conversationId: number): Observable<ApiResponse<ParticipantDto[]>> {
    return this.http.get<ApiResponse<ParticipantDto[]>>(`${this.baseUrl}/${conversationId}/participants`);
  }

  getUnreadCount(): Observable<ApiResponse<number>> {
    return this.http.get<ApiResponse<number>>(`${this.baseUrl}/my/unread-count`);
  }

  createDirectConversation(request: { targetUserId: number }): Observable<ApiResponse<ConversationDto>> {
    return this.http.post<ApiResponse<ConversationDto>>(`${this.baseUrl}/direct`, request);
  }

  createGroupConversation(request: { title: string; participantUserIds: number[] }): Observable<ApiResponse<ConversationDto>> {
    return this.http.post<ApiResponse<ConversationDto>>(`${this.baseUrl}/group`, request);
  }

  createSubjectGroupConversation(request: { subjectId: number; classId: number; academicYearId: number; title?: string }): Observable<ApiResponse<ConversationDto>> {
    return this.http.post<ApiResponse<ConversationDto>>(`${this.baseUrl}/subject-group`, request);
  }

  createClassGroupConversation(request: { classId: number; academicYearId: number; title?: string }): Observable<ApiResponse<ConversationDto>> {
    return this.http.post<ApiResponse<ConversationDto>>(`${this.baseUrl}/class-group`, request);
  }

  updateTitle(conversationId: number, title: string): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.baseUrl}/${conversationId}/title`, { title });
  }

  deleteConversation(conversationId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${conversationId}`);
  }

  addParticipant(conversationId: number, participantUserId: number): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/${conversationId}/participants?participantUserId=${participantUserId}`, {});
  }

  removeParticipant(conversationId: number, participantUserId: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${conversationId}/participants/${participantUserId}`);
  }

  searchConversations(term: string): Observable<ApiResponse<ConversationDto[]>> {
    return this.http.get<ApiResponse<ConversationDto[]>>(`${this.baseUrl}/search`, { params: { term } });
  }
}
