import { Component, signal, inject, OnInit, AfterViewChecked, ElementRef, viewChild, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import {
  AiAgentChatService,
  AgentResponse,
  ConversationListItem,
  ConversationMessage,
} from '../../core/services/ai-agent-chat.service';
import { AuthService } from '../../core/services/auth.service';
import { RoleService } from '../../shared/role.service';
import { VoiceTranscriptionService } from '../../core/services/voice-transcription.service';
import { TextToSpeechService } from '../../core/services/text-to-speech.service';
import { buildApiUrl } from '../../core/utils/api-url';
import { catchError, finalize, of } from 'rxjs';

export interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  timestamp: Date;
  suggestedActions?: string[];
  additionalData?: Record<string, unknown>;
}

interface QuickPrompt {
  label: string;
  icon: string;
  message: string;
}

const QUICK_PROMPTS: Record<string, QuickPrompt[]> = {
  teacher: [
    { label: 'موادي', icon: 'book', message: 'اعرض المواد المتاحة بتاعتي' },
    { label: 'إنشاء امتحان', icon: 'assignment', message: 'عايز أعمل امتحان للوحدة الأولى' },
    { label: 'تقييمات', icon: 'stars', message: 'أظهر تقييمات الفصل' },
    { label: 'مصادر تعليمية', icon: 'menu_book', message: 'اقترح مصادر تعليمية' },
  ],
  student: [
    { label: 'شرح درس', icon: 'school', message: 'شرح درس في الرياضيات' },
    { label: 'تمارين', icon: 'exercise', message: 'عايز تمارين تدريبية' },
    { label: 'امتحاناتي', icon: 'calendar_month', message: 'الامتحانات القادمة' },
    { label: 'تقييمي', icon: 'assessment', message: 'تقييمي الدراسي' },
  ],
  parent: [
    { label: 'تقرير ابني', icon: 'child_care', message: 'تقرير عن مستوى ابني' },
    { label: 'الامتحانات', icon: 'calendar_month', message: 'الامتحانات القادمة لابني' },
    { label: 'نقاط الضعف', icon: 'trending_down', message: 'نقاط ضعف ابني' },
    { label: 'أنشطة', icon: 'celebration', message: 'أنشطة تعليمية مقترحة' },
  ],
  admin: [
    { label: 'تقارير', icon: 'assessment', message: 'التقارير المتاحة' },
    { label: 'إحصائيات', icon: 'bar_chart', message: 'إحصائيات عامة' },
  ],
};

@Component({
  selector: 'app-chat-ai',
  imports: [Sidebar, FormsModule, DatePipe],
  templateUrl: './chat-ai.html',
  styleUrl: './chat-ai.css',
})
export class ChatAi implements OnInit, AfterViewChecked, OnDestroy {
  private agentSvc = inject(AiAgentChatService);
  private voiceTranscribeSvc = inject(VoiceTranscriptionService);
  private ttsSvc = inject(TextToSpeechService);
  private auth = inject(AuthService);
  private roleService = inject(RoleService);

  chatContainer = viewChild<ElementRef>('chatContainer');

  // ── Chat state ──
  sidebarOpen = signal(false);
  messages = signal<ChatMessage[]>([]);
  inputText = signal('');
  isLoading = signal(false);
  errorMsg = signal('');

  private readonly STORAGE_KEY = 'ai_conversation_id';
  private readonly SUGGESTIONS_KEY = 'ai_last_suggestions';
  private readonly CONVERSATIONS_KEY = 'ai_conversations_cache';
  conversationId = signal<string>('');
  /** مانع الإرسال المتزامن — يمنع إرسال طلبين لنفس المحادثة */
  private isSending = false;
  private lastSendTime = 0;

  private get storageKey(): string {
    return `${this.STORAGE_KEY}_${this.userRole}`;
  }

  private get suggestionsKey(): string {
    return `${this.SUGGESTIONS_KEY}_${this.userRole}`;
  }

  // ── History ──
  conversations = signal<ConversationListItem[]>([]);
  showHistory = signal(false);

  // ── Voice state ──
  isListening = signal(false);
  isRecordingBackend = signal(false);
  voiceSupported = VoiceTranscriptionService.isBrowserSpeechSupported();
  mediaRecorderSupported = VoiceTranscriptionService.isMediaRecorderSupported();

  private recognition: any = null;
  private voiceTimeout: any = null;
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];

  userName: string;
  userRole = '';
  quickPrompts: QuickPrompt[] = [];
  contextActions = signal<string[]>([]);
  ttsLoading = signal<string | null>(null); // holds the text being spoken, or null

  constructor() {
    const user = this.auth.user();
    this.userName = user?.fullName ?? 'مستخدم';
    const role = this.roleService.currentRole() ?? 'teacher';
    this.userRole = role;
    this.quickPrompts = QUICK_PROMPTS[role] ?? QUICK_PROMPTS['teacher'];

    if (this.voiceSupported) {
      this.initBrowserSpeechRecognition();
    }
  }

  ngOnInit() {
    // Try to restore previous conversationId from localStorage
    const stored = localStorage.getItem(this.storageKey);
    if (stored) {
      this.conversationId.set(stored);
      this.loadConversationMessages(stored);
    } else {
      this.conversationId.set(this.generateId());
      this.messages.set([]);
      this.contextActions.set(this.loadSavedSuggestions());
    }

    this.loadConversations();
  }

  /** حفظ آخر suggestedActions في localStorage */
  private saveSuggestions(actions: string[]) {
    if (actions && actions.length > 0) {
      localStorage.setItem(this.suggestionsKey, JSON.stringify(actions));
    } else {
      localStorage.removeItem(this.suggestionsKey);
    }
  }

  /** استرجاع آخر suggestedActions من localStorage */
  private loadSavedSuggestions(): string[] {
    try {
      const saved = localStorage.getItem(this.suggestionsKey);
      return saved ? JSON.parse(saved) : [];
    } catch {
      return [];
    }
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  ngOnDestroy() {
    this.stopBrowserSpeech();
    this.stopBackendRecording();
  }

  // ═══════════════════════════════════════════
  //  CONVERSATION HISTORY
  // ═══════════════════════════════════════════

  private generateId(): string {
    const id = 'conv_' + Date.now() + '_' + Math.random().toString(36).slice(2, 9);
    localStorage.setItem(this.storageKey, id);
    return id;
  }

  /** جلب قائمة المحادثات السابقة من السيرفر */
  loadConversations() {
    this.agentSvc.getConversations(this.userRole).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.conversations.set(res.data);
        }
      },
      error: () => {
        // Ignore errors silently
      },
    });
  }

  /** جلب رسائل محادثة معينة من السيرفر */
  private loadConversationMessages(convId: string) {
    this.agentSvc.getConversationMessages(convId).subscribe({
      next: (res) => {
        if (res.isSuccess && res.data.length > 0) {
          this.messages.set(
            res.data.map((m: ConversationMessage) => ({
              role: m.sender as 'user' | 'assistant',
              text: m.content,
              timestamp: new Date(m.timestamp),
            }))
          );
          // Restore last suggestions from localStorage
          const saved = this.loadSavedSuggestions();
          if (saved.length > 0) {
            this.contextActions.set(saved);
          }
        } else {
          this.messages.set([]);
          this.contextActions.set(this.loadSavedSuggestions());
        }
      },
      error: () => {
        this.conversationId.set(this.generateId());
        this.messages.set([]);
        this.contextActions.set(this.loadSavedSuggestions());
      },
    });
  }

  /** التبديل إلى محادثة سابقة */
  switchConversation(convId: string) {
    this.showHistory.set(false);
    this.conversationId.set(convId);
    localStorage.setItem(this.storageKey, convId);
    this.isLoading.set(true);

    this.loadConversationMessages(convId);
    this.isLoading.set(false);
  }

  /** بدء محادثة جديدة */
  newConversation() {
    this.showHistory.set(false);
    this.conversationId.set(this.generateId());
    this.messages.set([]);
    localStorage.removeItem(this.suggestionsKey);
    this.contextActions.set([]);
  }

  /** حذف محادثة */
  deleteConversation(convId: string, event: MouseEvent) {
    event.stopPropagation();
    if (!confirm('هل أنت متأكد من حذف هذه المحادثة؟')) return;

    this.agentSvc.deleteConversation(convId).subscribe({
      next: () => {
        // Remove from local list
        this.conversations.update(list => list.filter(c => c.conversationId !== convId));
        // If it was the active conversation, reset
        if (this.conversationId() === convId) {
          this.conversationId.set(this.generateId());
          this.messages.set([]);
          this.contextActions.set([]);
        }
      },
      error: () => {
        this.errorMsg.set('فشل حذف المحادثة');
      },
    });
  }

  // ═══════════════════════════════════════════
  //  VOICE
  // ═══════════════════════════════════════════

  private initBrowserSpeechRecognition() {
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    if (!SpeechRecognition) return;

    this.recognition = new SpeechRecognition();
    this.recognition.continuous = true;
    this.recognition.interimResults = true;
    this.recognition.lang = 'ar-SA';
    this.recognition.maxAlternatives = 1;

    this.recognition.onresult = (event: any) => {
      let final = '';
      // Only process NEW results from resultIndex
      for (let i = event.resultIndex; i < event.results.length; i++) {
        if (event.results[i].isFinal) {
          final += event.results[i][0].transcript;
        }
      }
      if (final) {
        this.inputText.update(prev => prev + (prev ? ' ' : '') + final);
      }
    };

    this.recognition.onerror = (event: any) => {
      console.error('SpeechRecognition error:', event.error);

      if (event.error === 'not-allowed' || event.error === 'service-not-allowed') {
        // Fatal — stop trying
        this.voiceSupported = false;
        this.isListening.set(false);
        if (this.voiceTimeout) {
          clearTimeout(this.voiceTimeout);
          this.voiceTimeout = null;
        }
      } else if (event.error === 'network' || event.error === 'aborted') {
        // Browser has the class but service is unavailable → fallback to backend
        this.voiceSupported = false;
        this.isListening.set(false);
        if (this.voiceTimeout) {
          clearTimeout(this.voiceTimeout);
          this.voiceTimeout = null;
        }
        // Auto-fallback to backend recording
        this.startBackendRecording();
      }
    };

    this.recognition.onend = () => {
      // Auto-restart if user still wants listening and timeout hasn't expired
      if (this.voiceTimeout) {
        try { this.recognition?.start(); } catch { /* ignore */ }
      } else {
        this.isListening.set(false);
      }
    };
  }

  private startBrowserSpeech() {
    if (!this.recognition) return;
    try {
      this.recognition.lang = 'ar-SA';
      this.isListening.set(true);
      this.recognition.start();
      this.voiceTimeout = setTimeout(() => {
        this.stopBrowserSpeech();
      }, 15000);
    } catch (e) {
      console.error('Failed to start browser speech:', e);
      this.voiceSupported = false;
      this.isListening.set(false);
      // Fallback to backend recording immediately
      this.startBackendRecording();
    }
  }

  private stopBrowserSpeech() {
    // Clear timeout FIRST so onend won't auto-restart
    if (this.voiceTimeout) {
      clearTimeout(this.voiceTimeout);
      this.voiceTimeout = null;
    }
    if (this.recognition) {
      try { this.recognition.stop(); } catch { /* ignore */ }
    }
    this.isListening.set(false);
  }

  private async startBackendRecording() {
    if (!this.mediaRecorderSupported) return;

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioChunks = [];

      const mimeType = MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm' : 'audio/mp4';
      this.mediaRecorder = new MediaRecorder(stream, { mimeType });

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) this.audioChunks.push(event.data);
      };

      this.mediaRecorder.onstop = async () => {
        stream.getTracks().forEach(t => t.stop());

        const blob = new Blob(this.audioChunks, { type: mimeType });
        this.audioChunks = [];
        this.recordingDone(blob);
      };

      this.mediaRecorder.onerror = () => {
        stream.getTracks().forEach(t => t.stop());
        this.isRecordingBackend.set(false);
        this.errorMsg.set('فشل التسجيل الصوتي، حاول مرة أخرى.');
      };

      this.mediaRecorder.start();
      this.isRecordingBackend.set(true);

      setTimeout(() => {
        if (this.isRecordingBackend() && this.mediaRecorder?.state === 'recording') {
          this.mediaRecorder.stop();
        }
      }, 15000);
    } catch (err) {
      console.error('MediaRecorder error:', err);
      this.isRecordingBackend.set(false);
      this.errorMsg.set('لا يمكن الوصول إلى الميكروفون، تأكد من السماح بالتسجيل.');
    }
  }

  private stopBackendRecording() {
    if (this.mediaRecorder && this.mediaRecorder.state === 'recording') {
      this.mediaRecorder.stop();
    }
    this.isRecordingBackend.set(false);
  }

  private recordingDone(blob: Blob) {
    this.isRecordingBackend.set(false);

    if (blob.size < 100) {
      this.errorMsg.set('التسجيل قصير جداً، حاول مرة أخرى.');
      return;
    }

    this.isLoading.set(true);

    this.voiceTranscribeSvc.transcribeAudio(blob).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.isSuccess && res.text) {
          this.inputText.set(this.inputText() + (this.inputText() ? ' ' : '') + res.text);
        } else {
          this.errorMsg.set(res.message || 'لم يتم التعرف على الكلام.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMsg.set('حدث خطأ أثناء معالجة الصوت.');
        console.error('Transcription upload error:', err);
      },
    });
  }

  toggleVoiceInput() {
    if (this.isListening()) {
      this.stopBrowserSpeech();
      return;
    }
    if (this.isRecordingBackend()) {
      this.stopBackendRecording();
      return;
    }

    if (this.voiceSupported) {
      this.startBrowserSpeech();
    } else if (this.mediaRecorderSupported) {
      this.startBackendRecording();
    } else {
      this.errorMsg.set('التسجيل الصوتي غير مدعوم في متصفحك.');
    }
  }

  get isVoiceActive(): boolean {
    return this.isListening() || this.isRecordingBackend();
  }

  getVoiceTooltip(): string {
    if (this.isRecordingBackend()) return 'جاري التسجيل ... اضغط للإيقاف';
    if (this.isListening()) return 'اضغط لإيقاف التسجيل';
    if (this.voiceSupported) return 'اضغط للتحدث (التعرف التلقائي)';
    return 'اضغط للتسجيل وإرسال الصوت';
  }

  // ═══════════════════════════════════════════
  //  CORE CHAT
  // ═══════════════════════════════════════════

  private scrollToBottom() {
    const el = this.chatContainer()?.nativeElement;
    if (el) {
      setTimeout(() => { el.scrollTop = el.scrollHeight; }, 50);
    }
  }

  private addUserMessage(text: string) {
    this.messages.update(msgs => [
      ...msgs,
      { role: 'user' as const, text, timestamp: new Date() },
    ]);
  }

  private addAssistantMessage(text: string, suggestedActions?: string[], additionalData?: Record<string, unknown>) {
    this.messages.update(msgs => [
      ...msgs,
      {
        role: 'assistant' as const,
        text,
        timestamp: new Date(),
        suggestedActions,
        additionalData,
      },
    ]);
    if (suggestedActions && suggestedActions.length > 0) {
      this.contextActions.set(suggestedActions);
      this.saveSuggestions(suggestedActions);
    }
  }

  sendMessage(text?: string) {
    const message = text ?? this.inputText();
    if (!message || !message.trim() || this.isLoading()) return;

    // Debounce: يمنع تكرار الإرسال خلال ثانية واحدة
    const now = Date.now();
    if (this.lastSendTime > 0 && now - this.lastSendTime < 1000) return;
    this.lastSendTime = now;

    const trimmed = message.trim();
    this.inputText.set('');
    this.addUserMessage(trimmed);
    this.isLoading.set(true);
    this.isSending = true;
    this.errorMsg.set('');

    this.agentSvc
      .chat(trimmed, this.conversationId())
      .pipe(
        catchError(err => {
          const msg = err.error?.message || err.message || 'حدث خطأ في الاتصال بالمساعد';
          this.errorMsg.set(msg);
          return of(null);
        }),
        finalize(() => {
          this.isLoading.set(false);
          this.isSending = false;
        })
      )
      .subscribe({
        next: result => {
          if (!result || !result.isSuccess) {
            this.addAssistantMessage('عذراً، لم يتمكن المساعد من الإجابة. حاول مرة أخرى.');
            return;
          }
          const data: AgentResponse = result.data;
          this.addAssistantMessage(data.text, data.suggestedActions, data.additionalData);

          if (data.additionalData?.['conversationId']) {
            const newId = data.additionalData['conversationId'] as string;
            this.conversationId.set(newId);
            localStorage.setItem(this.storageKey, newId);
          }

          // Refresh conversation list after sending a message
          this.loadConversations();
        },
      });
  }

  onSuggestedAction(action: string) {
    this.sendMessage(action);
  }

  /** تحويل نص الرسالة إلى صوت (لطلاب فقط) */
  speakMessage(text: string) {
    if (this.ttsLoading() !== null) return; // already playing
    this.ttsLoading.set(text);
    this.isSending = true; // Use same debounce mechanism
    this.ttsSvc.synthesizeSpeech(text).subscribe({
      next: (blob) => {
        this.ttsLoading.set(null);
        this.isSending = false;
        const audioUrl = URL.createObjectURL(blob);
        const audio = new Audio(audioUrl);
        audio.onended = () => URL.revokeObjectURL(audioUrl);
        audio.play().catch(() => {
          // Autoplay might be blocked
        });
      },
      error: () => {
        this.ttsLoading.set(null);
        this.isSending = false;
        this.errorMsg.set('فشل تحويل النص إلى صوت');
      },
    });
  }

  /** استخراج الروابط من النص */
  extractUrls(text: string): string[] {
    const urls: string[] = [];
    const regex = /(\/(?:api|exam)\/[^\s\)\]\}\]\<\`]+|https?:\/\/[^\s\)\]\}\]\<\`]+)/g;
    const matches = text.matchAll(regex);
    for (const m of matches) {
      let url = m[1];
      // إزالة أي علامات ترقيم أو باكتيك في النهاية
      const cleanUrl = url.replace(/[`\)\]\}>"'.,;:!?]+$/, '');
      if (!urls.includes(cleanUrl)) urls.push(cleanUrl);
    }
    return urls;
  }

  /**
   * تحليل نص الرسالة وتحويل الخيارات إلى أزرار قابلة للنقر
   * فقط الأسطر اللي تبدأ بـ 🔹 تتحول لأزرار
   * وأي رابط /exam/... أو /api/... يتشال من النص ويظهر كزرار بره
   */
  parseMessage(text: string): { type: 'text' | 'option'; text: string }[] {
    const parts: { type: 'text' | 'option'; text: string }[] = [];
    const lines = text.split('\n');

    // نمط لإزالة الروابط من نص السطور
    const urlPattern = /\/?(api|exam)\/\S+/g;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();

      // فقط 🔹 يتحول لزرار - وخلاص
      if (trimmed.startsWith('🔹')) {
        const optionText = trimmed.replace(/^🔹\s*/, '').replace(urlPattern, '').trim();
        if (optionText) {
          if (parts.length > 0 && parts[parts.length - 1].type === 'text') {
            parts[parts.length - 1].text += '\n';
          }
          parts.push({ type: 'option', text: optionText });
        } else {
          if (parts.length > 0 && parts[parts.length - 1].type === 'text') {
            parts[parts.length - 1].text += (i > 0 ? '\n' : '') + line;
          } else {
            parts.push({ type: 'text', text: line });
          }
        }
      } else {
        // شيل الروابط من النص (هتظهر كزرار معاينة)
        const cleanedLine = line.replace(urlPattern, '').trim();
        if (!cleanedLine) continue; // السطر كان مجرد رابط

        // Merge consecutive text lines
        if (parts.length > 0 && parts[parts.length - 1].type === 'text') {
          parts[parts.length - 1].text += (i > 0 ? '\n' : '') + cleanedLine;
        } else {
          parts.push({ type: 'text', text: cleanedLine });
        }
      }
    }

    return parts;
  }

  /** فتح رابط في تبويب جديد */
  openUrl(url: string) {
    // Remove leading /api/ if present (buildApiUrl already adds /api/)
    const cleanUrl = url.replace(/^\/api\//, '').replace(/^\//, '');
    const fullUrl = url.startsWith('http') ? url : buildApiUrl(cleanUrl);
    window.open(fullUrl, '_blank');
  }

  onQuickPrompt(prompt: QuickPrompt) {
    this.sendMessage(prompt.message);
  }

  get isTeacher(): boolean {
    return this.userRole === 'teacher';
  }

  get isParent(): boolean {
    return this.userRole === 'parent';
  }

  get isStudent(): boolean {
    return this.userRole === 'student';
  }

  /** عدد المحادثات السابقة */
  get conversationCount(): number {
    return this.conversations().length;
  }
}
