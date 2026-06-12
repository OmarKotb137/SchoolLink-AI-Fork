import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';
import { RoleService } from '../../shared/role.service';

export interface AgentChatRequest {
  message: string;
  conversationId?: string;
}

export interface AgentResponse {
  text: string;
  suggestedActions: string[];
  additionalData?: Record<string, unknown>;
}

export interface ApiResult<T> {
  isSuccess: boolean;
  message: string;
  statusCode: number;
  data: T;
}

export interface ConversationListItem {
  conversationId: string;
  agentType: string;
  summary: string;
  lastMessageAt: string;
  messageCount: number;
}

export interface ConversationMessage {
  sender: string;
  content: string;
  timestamp: string;
}

const AGENT_ROUTES: Record<string, string> = {
  parent: 'parent-agent',
  student: 'student-agent',
  teacher: 'teacher-agent',
};

@Injectable({ providedIn: 'root' })
export class AiAgentChatService {
  private http = inject(HttpClient);
  private roleService = inject(RoleService);

  private getAgentPath(): string {
    const role = this.roleService.currentRole();
    const path = role ? AGENT_ROUTES[role] : null;
    if (!path) throw new Error(`No AI agent configured for role: ${role}`);
    return path;
  }

  chat(message: string, conversationId?: string): Observable<ApiResult<AgentResponse>> {
    const path = this.getAgentPath();
    const body: AgentChatRequest = { message, conversationId };
    return this.http.post<ApiResult<AgentResponse>>(
      buildApiUrl(`ai/${path}/chat`),
      body
    );
  }

  /** جلب قائمة المحادثات السابقة للمستخدم الحالي */
  getConversations(agentType?: string): Observable<ApiResult<ConversationListItem[]>> {
    const params = agentType ? `?agentType=${agentType}` : '';
    return this.http.get<ApiResult<ConversationListItem[]>>(
      buildApiUrl(`ai/chats${params}`)
    );
  }

  /** جلب رسائل محادثة محددة */
  getConversationMessages(conversationId: string): Observable<ApiResult<ConversationMessage[]>> {
    return this.http.get<ApiResult<ConversationMessage[]>>(
      buildApiUrl(`ai/chats/${encodeURIComponent(conversationId)}/messages`)
    );
  }

  /** حذف محادثة كاملة */
  deleteConversation(conversationId: string): Observable<ApiResult<any>> {
    return this.http.delete<ApiResult<any>>(
      buildApiUrl(`ai/chats/${encodeURIComponent(conversationId)}`)
    );
  }
}
