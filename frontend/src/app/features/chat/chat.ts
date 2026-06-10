import { Component, signal, computed, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { ConversationService, ConversationDto, MessageDto, ParticipantDto } from '../../core/services/conversation.service';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { ClassService } from '../../core/services/class.service';
import { SubjectService } from '../../core/services/subject.service';
import { AcademicYearService } from '../../core/services/academic-year.service';
import { RoleService } from '../../shared/role.service';
import { getBackendBaseUrl } from '../../core/utils/api-url';

interface ChatMessage {
  id: number;
  senderId: number;
  senderName: string;
  senderRole: string;
  content: string;
  sentAt: string;
  isMine: boolean;
  isEdited: boolean;
  attachmentUrl?: string;
  attachmentType?: string;
}

interface ChatConversation {
  id: number;
  name: string;
  type: 'individual' | 'group';
  lastMessage: string;
  lastTime: string;
  unread: boolean;
  participants: ParticipantDto[];
  messages: ChatMessage[];
}

@Component({
  selector: 'app-chat',
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './chat.html',
  styleUrl: './chat.css',
})
export class Chat implements OnInit, OnDestroy {
  conversationService = inject(ConversationService);
  authService = inject(AuthService);
  private userService = inject(UserService);
  private classService = inject(ClassService);
  private subjectService = inject(SubjectService);
  private academicYearService = inject(AcademicYearService);
  roleService = inject(RoleService);

  sidebarOpen = signal(false);
  showConvList = signal(true);
  activeFilter = signal<'all' | 'groups' | 'individual'>('all');
  activeConversationId = signal<number | null>(null);
  newMessage = signal('');
  searchTerm = signal('');
  loadingConversations = signal(false);
  loadingMessages = signal(false);
  loadingOlder = signal(false);
  hasMoreMessages = signal(true);
  messagesPage = signal(1);
  pageSize = 20;
  totalUnread = signal(0);

  conversations = signal<ChatConversation[]>([]);

  activeConversation = computed(() =>
    this.conversations().find(c => c.id === this.activeConversationId()) ?? null
  );
  isBlocked = signal(false);
  blockedByOther = signal(false);
  blockedByMe = signal<Set<number>>(new Set());

  filteredConversations = computed(() => {
    const filter = this.activeFilter();
    const term = this.searchTerm().toLowerCase();
    let list = this.conversations();
    if (filter === 'groups') list = list.filter(c => c.type === 'group');
    else if (filter === 'individual') list = list.filter(c => c.type === 'individual');
    if (term) list = list.filter(c => c.name.toLowerCase().includes(term));
    return list;
  });

  showProfilePanel = signal(false);
  showCreateGroupModal = signal(false);
  groupType = signal<'all-subjects' | 'specific-subject'>('all-subjects');
  groupTitle = signal('');
  selectedSubjectId = signal<number | null>(null);
  selectedClassId = signal<number | null>(null);
  selectedAcademicYearId = signal<number | null>(null);
  creatingGroup = signal(false);
  classList = signal<any[]>([]);
  subjectList = signal<any[]>([]);
  academicYearList = signal<any[]>([]);

  showUserSearch = signal(false);
  userSearchQuery = signal('');
  userSearchResults = signal<any[]>([]);
  userSearchTimeout: any = null;

  showAddParticipantModal = signal(false);
  searchUserQuery = signal('');
  searchUserResults = signal<any[]>([]);
  addingParticipant = signal(false);
  removingParticipantId = signal<number | null>(null);
  searchTimeout: any = null;

  typingTimeout: any = null;
  private msgSub?: Subscription;
  private updateSub?: Subscription;
  private deleteSub?: Subscription;
  editingMessageId = signal<number | null>(null);
  editingMessageContent = signal('');
  errorMessage = signal<string | null>(null);
  selectedFile = signal<File | null>(null);
  uploadingFile = signal(false);
  smartSuggestions = signal([
    'شكراً جزيلاً، هل هناك أي تحديثات أخرى؟',
    'تم الاستلام، سنتابع مع الطالب',
    'هل تحتاج منا أي متابعة إضافية؟',
  ]);

  ngOnInit(): void {
    this.loadConversations();
    this.conversationService.startConnection();
    this.loadUnreadCount();

    this.msgSub = this.conversationService.receivedMessage$.subscribe(msg => {
      const activeId = this.activeConversationId();
      this.conversations.update(list => list.map(c => {
        if (c.id !== msg.conversationId) return c;
        if (c.messages.some(m => m.id === msg.id)) return c;
        return {
          ...c,
          messages: c.id === activeId ? [...c.messages, this.toChatMessage(msg, c.participants)] : c.messages,
          lastMessage: msg.content,
          lastTime: this.formatTime(msg.sentAt),
          unread: c.id !== activeId,
        };
      }));
    });

    this.updateSub = this.conversationService.updatedMessage$.subscribe(msg => {
      this.conversations.update(list => list.map(c => {
        if (c.id !== msg.conversationId) return c;
        return {
          ...c,
          messages: c.messages.map(m => m.id === msg.id
            ? { ...m, content: msg.content, isEdited: true }
            : m),
        };
      }));
    });

    this.deleteSub = this.conversationService.deletedMessage$.subscribe(({ conversationId, messageId }) => {
      this.conversations.update(list => list.map(c => {
        if (c.id !== conversationId) return c;
        return { ...c, messages: c.messages.filter(m => m.id !== messageId) };
      }));
    });
  }

  ngOnDestroy(): void {
    this.msgSub?.unsubscribe();
    this.updateSub?.unsubscribe();
    this.deleteSub?.unsubscribe();
    this.conversationService.stopConnection();
  }

  loadConversations(): void {
    this.loadingConversations.set(true);
    this.conversationService.getMyConversations().subscribe({
      next: res => {
        if (res.isSuccess && res.data) {
          this.conversations.set(res.data.map(dto => this.toChatConversation(dto)));
        }
      },
      complete: () => this.loadingConversations.set(false),
    });
  }

  refreshConversations(): void {
    this.loadConversations();
    this.loadUnreadCount();
  }

  loadUnreadCount(): void {
    this.conversationService.getUnreadCount().subscribe(res => {
      if (res.isSuccess) this.totalUnread.set(res.data);
    });
  }

  selectConversation(id: number): void {
    const prev = this.activeConversationId();
    if (prev) this.conversationService.leaveConversation(prev);
    this.activeConversationId.set(id);
    this.conversationService.joinConversation(id);
    this.conversationService.markAsReadRest(id).subscribe();
    this.conversationService.markAsReadSignalR(id);
    this.conversations.update(list => list.map(c => c.id === id ? { ...c, unread: false } : c));
    this.loadMessages(id);
    this.showConvList.set(false);
    this.sidebarOpen.set(false);
    this.showUserSearch.set(false);
    this.userSearchQuery.set('');
    this.userSearchResults.set([]);
    this.checkBlockStatus();
  }

  private checkBlockStatus(): void {
    this.isBlocked.set(false);
    this.blockedByOther.set(false);
    const conv = this.activeConversation();
    if (!conv || conv.type === 'group') return;
    const currentUserId = this.authService.user()?.userId;
    const other = conv.participants.find(p => p.userId !== currentUserId);
    if (!other) return;
    this.conversationService.isUserBlocked(conv.id, other.userId).subscribe({
      next: res => {
        if (res.isSuccess) {
          this.isBlocked.set(res.data);
          this.blockedByOther.set(res.data);
        }
      },
      error: () => {},
    });
  }

  backToConversations(): void {
    this.showConvList.set(true);
  }

  toggleProfilePanel(): void {
    this.showProfilePanel.update(v => !v);
  }

  loadMessages(conversationId: number): void {
    this.loadingMessages.set(true);
    this.hasMoreMessages.set(true);
    this.messagesPage.set(1);
    this.conversationService.getMessages(conversationId, 1, this.pageSize).subscribe({
      next: res => {
        if (res.isSuccess && res.data) {
          this.hasMoreMessages.set(res.data!.page < res.data!.totalPages);
          const items = (res.data!.items ?? []).reverse();
          this.conversations.update(list => list.map(c => {
            if (c.id === conversationId) {
              return { ...c, messages: items.map(m => this.toChatMessage(m, c.participants)) };
            }
            return c;
          }));
        }
      },
      complete: () => this.loadingMessages.set(false),
    });
  }

  loadOlderMessages(conversationId: number): void {
    if (this.loadingOlder() || !this.hasMoreMessages()) return;
    this.loadingOlder.set(true);
    const nextPage = this.messagesPage() + 1;
    this.conversationService.getMessages(conversationId, nextPage, this.pageSize).subscribe({
      next: res => {
        if (res.isSuccess && res.data) {
          this.hasMoreMessages.set(res.data!.page < res.data!.totalPages);
          this.messagesPage.set(nextPage);
          const participants = this.activeConversation()?.participants ?? [];
          const items = (res.data!.items ?? []).reverse().map(m => this.toChatMessage(m, participants));
          this.conversations.update(list => list.map(c => {
            if (c.id === conversationId) {
              return { ...c, messages: [...items, ...c.messages] };
            }
            return c;
          }));
        }
      },
      complete: () => this.loadingOlder.set(false),
    });
  }

  onMessagesScroll(event: Event): void {
    const el = event.target as HTMLElement;
    if (el.scrollTop < 80) {
      const convId = this.activeConversationId();
      if (convId) this.loadOlderMessages(convId);
    }
  }

  sendMessage(): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    const file = this.selectedFile();
    if (file) {
      const text = this.newMessage().trim();
      this.newMessage.set('');
      this.sendWithFile(file, text);
      return;
    }
    const text = this.newMessage().trim();
    if (!text) return;
    this.newMessage.set('');
    this.conversationService.sendMessageRest(convId, text).subscribe({
      next: res => {
        if (res.isSuccess && res.data) {
          this.appendMessage(convId, this.toChatMessage(res.data, this.activeConversation()?.participants));
        } else {
          this.showError(res.message || 'حدث خطأ أثناء الإرسال');
          this.newMessage.set(text);
        }
      },
      error: (err: any) => {
        const msg = err?.error?.message || err?.message || 'حدث خطأ في الاتصال';
        this.showError(msg);
        this.newMessage.set(text);
      },
    });
  }

  private appendMessage(convId: number, msg: ChatMessage): void {
    this.conversations.update(list => list.map(c => {
      if (c.id !== convId) return c;
      if (c.messages.some(m => m.id === msg.id)) return c;
      return {
        ...c,
        messages: [...c.messages, { ...msg, senderRole: this.getSenderRole(msg.senderId, c.participants) }],
        lastMessage: msg.content,
        lastTime: this.formatTime(msg.sentAt),
      };
    }));
  }

  onTyping(): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    this.conversationService.sendTypingSignalR(convId);
    if (this.typingTimeout) clearTimeout(this.typingTimeout);
    this.typingTimeout = setTimeout(() => {
      this.conversationService.sendStoppedTypingSignalR(convId);
    }, 2000);
  }

  useSuggestion(text: string): void {
    this.newMessage.set(text);
  }

  setActiveFilter(filter: 'all' | 'groups' | 'individual'): void {
    this.activeFilter.set(filter);
  }

  openCreateGroupModal(): void {
    this.groupType.set('all-subjects');
    this.groupTitle.set('');
    this.selectedSubjectId.set(null);
    this.selectedClassId.set(null);
    this.selectedAcademicYearId.set(null);
    this.showCreateGroupModal.set(true);
    this.academicYearService.getCurrent().subscribe(res => {
      const data = res.data ?? res;
      if (data?.id) this.selectedAcademicYearId.set(data.id);
    });
    this.academicYearService.getAll().subscribe(res => {
      const data = res.data ?? res;
      this.academicYearList.set(Array.isArray(data) ? data : []);
    });
    this.classService.getAll().subscribe(res => {
      const data = res.data ?? res;
      if (Array.isArray(data)) this.classList.set(data);
      else if (data?.items) this.classList.set(data.items);
    });
    this.subjectService.getAll().subscribe(res => {
      const data = res.data ?? res;
      this.subjectList.set(Array.isArray(data) ? data : []);
    });
  }

  closeCreateGroupModal(): void {
    this.showCreateGroupModal.set(false);
  }

  createGroup(): void {
    const classId = this.selectedClassId();
    const academicYearId = this.selectedAcademicYearId();
    if (!classId || !academicYearId) return;
    const gType = this.groupType();
    if (gType === 'specific-subject' && !this.selectedSubjectId()) return;
    this.creatingGroup.set(true);
    const obs = gType === 'specific-subject'
      ? this.conversationService.createSubjectGroupConversation({
          subjectId: this.selectedSubjectId()!,
          classId,
          academicYearId,
          title: this.groupTitle() || undefined,
        })
      : this.conversationService.createClassGroupConversation({
          classId,
          academicYearId,
          title: this.groupTitle() || undefined,
        });
    obs.subscribe({
      next: res => {
        if (res.isSuccess && res.data) {
          this.conversations.update(list => [this.toChatConversation(res.data!), ...list]);
          this.selectConversation(res.data!.id);
          this.closeCreateGroupModal();
        }
      },
      complete: () => this.creatingGroup.set(false),
    });
  }

  isAdminOrTeacher(): boolean {
    const role = this.authService.user()?.role;
    return role === 'admin' || role === 'teacher';
  }

  canBlockUser(targetRole: string): boolean {
    const myRole = this.authService.user()?.role;
    if (!myRole) return false;
    if (myRole === 'admin') return true;
    if (myRole === 'teacher') return targetRole !== 'admin';
    if (myRole === 'student') return targetRole === 'student' || targetRole === 'parent';
    return false;
  }

  openDirectConversation(userId: number): void {
    const currentUserId = this.authService.user()?.userId;
    if (!currentUserId || userId === currentUserId) return;
    const existing = this.conversations().find(c =>
      c.type === 'individual' && c.participants.some(p => p.userId === userId)
    );
    if (existing) {
      this.selectConversation(existing.id);
      return;
    }
    this.conversationService.createDirectConversation({ targetUserId: userId }).subscribe(res => {
      if (res.isSuccess && res.data) {
        this.conversations.update(list => [this.toChatConversation(res.data!), ...list]);
        this.selectConversation(res.data!.id);
      }
    });
  }

  searchUsersForChat(): void {
    if (this.userSearchTimeout) clearTimeout(this.userSearchTimeout);
    const q = this.userSearchQuery().trim();
    if (!q || q.length < 2) {
      this.userSearchResults.set([]);
      return;
    }
    this.userSearchTimeout = setTimeout(() => {
      this.userService.search(q, 20).subscribe({
        next: res => {
          const data = res.data ?? res;
          const items: any[] = Array.isArray(data) ? data : data?.items ?? [];
          const currentUserId = this.authService.user()?.userId;
          const seen = new Set<number>();
          this.userSearchResults.set(items.filter((u: any) => {
            if (u.id === currentUserId) return false;
            if (seen.has(u.id)) return false;
            seen.add(u.id);
            return true;
          }));
        },
        error: () => {},
      });
    }, 300);
  }

  openAddParticipantModal(): void {
    this.showAddParticipantModal.set(true);
    this.searchUserQuery.set('');
    this.searchUserResults.set([]);
  }

  closeAddParticipantModal(): void {
    this.showAddParticipantModal.set(false);
    this.searchUserQuery.set('');
    this.searchUserResults.set([]);
  }

  searchUsers(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    const q = this.searchUserQuery().trim();
    if (!q || q.length < 2) {
      this.searchUserResults.set([]);
      return;
    }
    this.searchTimeout = setTimeout(() => {
      this.userService.search(q, 50).subscribe({
        next: res => {
          const data = res.data ?? res;
          const items: any[] = Array.isArray(data) ? data : data?.items ?? [];
          const conv = this.activeConversation();
          const existingIds = new Set(conv?.participants.map(p => p.userId) ?? []);
          const seen = new Set<number>();
          this.searchUserResults.set(items.filter((u: any) => {
            if (existingIds.has(u.id)) return false;
            if (seen.has(u.id)) return false;
            seen.add(u.id);
            return true;
          }));
        },
        error: () => {},
      });
    }, 300);
  }

  addParticipant(userId: number): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    this.addingParticipant.set(true);
    this.conversationService.addParticipant(convId, userId).subscribe({
      next: res => {
        if (res.isSuccess) {
          this.conversations.update(list => list.map(c => {
            if (c.id === convId) {
              const added = this.searchUserResults().find(u => u.id === userId);
              const newParticipant: ParticipantDto = {
                id: 0,
                userId: added?.id ?? userId,
                userName: added?.fullName ?? 'مستخدم',
                role: added?.role ?? '',
                joinedAt: new Date().toISOString(),
                lastReadAt: null,
              };
              return { ...c, participants: [...c.participants, newParticipant] };
            }
            return c;
          }));
          this.searchUserResults.update(list => list.filter(u => u.id !== userId));
        }
      },
      complete: () => this.addingParticipant.set(false),
    });
  }

  removeParticipant(userId: number): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    this.removingParticipantId.set(userId);
    this.conversationService.removeParticipant(convId, userId).subscribe({
      next: () => {
        this.conversations.update(list => list.map(c => {
          if (c.id === convId) {
            return { ...c, participants: c.participants.filter(p => p.userId !== userId) };
          }
          return c;
        }));
      },
      complete: () => this.removingParticipantId.set(null),
    });
  }

  startEditMessage(msg: ChatMessage): void {
    this.editingMessageId.set(msg.id);
    this.editingMessageContent.set(msg.content);
  }

  cancelEditMessage(): void {
    this.editingMessageId.set(null);
    this.editingMessageContent.set('');
  }

  handleEditKeydown(event: Event): void {
    const kbEvent = event as KeyboardEvent;
    if (kbEvent.shiftKey) return;
    kbEvent.preventDefault();
    this.saveEditMessage();
  }

  saveEditMessage(): void {
    const convId = this.activeConversationId();
    const msgId = this.editingMessageId();
    const content = this.editingMessageContent().trim();
    if (!convId || !msgId || !content) return;
    this.conversationService.updateMessage(convId, msgId, content).subscribe({
      next: () => {
        this.cancelEditMessage();
      },
      error: () => {},
    });
  }

  deleteMessage(convId: number, msgId: number): void {
    if (!confirm('هل أنت متأكد من حذف هذه الرسالة؟')) return;
    this.conversationService.deleteMessage(convId, msgId).subscribe({ error: () => {} });
  }

  selectedFileName = computed(() => this.selectedFile()?.name ?? null);

  handleFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.selectedFile.set(input.files[0]);
    }
    input.value = '';
  }

  clearSelectedFile(): void {
    this.selectedFile.set(null);
  }

  private sendWithFile(file: File, text: string): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    this.uploadingFile.set(true);
    this.conversationService.uploadFile(file).subscribe({
      next: res => {
        if (res.isSuccess && res.data) {
          const content = text || res.data.fileName;
          this.conversationService.sendMessageRest(convId, content, res.data.url, file.type || 'application/octet-stream').subscribe({
            next: msgRes => {
              if (msgRes.isSuccess && msgRes.data) {
                this.appendMessage(convId, this.toChatMessage(msgRes.data, this.activeConversation()?.participants));
              } else {
                this.showError(msgRes.message || 'حدث خطأ أثناء الإرسال');
              }
            },
            error: () => this.showError('حدث خطأ أثناء إرسال الملف'),
            complete: () => {
              this.uploadingFile.set(false);
              this.selectedFile.set(null);
            },
          });
        } else {
          this.uploadingFile.set(false);
        }
      },
      error: () => {
        this.showError('حدث خطأ أثناء رفع الملف');
        this.uploadingFile.set(false);
      },
    });
  }

  uploadFile(): void {
    const file = this.selectedFile();
    if (!file) return;
    const text = this.newMessage().trim();
    this.newMessage.set('');
    this.sendWithFile(file, text);
  }

  blockUserInConversation(blockedUserId: number): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    if (!confirm('هل أنت متأكد من حظر هذا المستخدم؟')) return;
    this.conversationService.blockUser(convId, blockedUserId).subscribe({
      next: res => {
        if (res.isSuccess) {
          this.blockedByMe.update(s => new Set(s).add(blockedUserId));
          this.isBlocked.set(true);
          this.showError('تم حظر المستخدم بنجاح');
        } else {
          this.showError(res.message || 'حدث خطأ');
        }
      },
      error: (err: any) => this.showError(err?.error?.message || 'حدث خطأ'),
    });
  }

  fileTypeIcon(type?: string): string {
    if (!type) return 'insert_drive_file';
    if (type.startsWith('image/')) return 'image';
    if (type.includes('pdf')) return 'picture_as_pdf';
    if (type.includes('word') || type.includes('document')) return 'description';
    if (type.includes('sheet') || type.includes('excel')) return 'table_chart';
    if (type.includes('video')) return 'videocam';
    if (type.includes('audio')) return 'audiotrack';
    return 'insert_drive_file';
  }

  uploadUrl(path: string): string {
    if (path.startsWith('http')) return path;
    return `${getBackendBaseUrl()}${path}`;
  }

  openImageInNewTab(url: string): void {
    window.open(this.uploadUrl(url), '_blank');
  }

  isImageFile(url: string): boolean {
    return /\.(jpg|jpeg|png|gif|webp|bmp|svg)$/i.test(url);
  }

  getFileName(url: string): string {
    const name = url.split('/').pop() ?? 'ملف';
    return name;
  }

  getFileTitle(msg: ChatMessage): string {
    if (!msg.attachmentUrl) return '';
    const ext = msg.attachmentUrl.split('.').pop()?.toLowerCase() ?? '';
    const typeMap: Record<string, string> = {
      pdf: 'PDF', jpg: 'صورة', jpeg: 'صورة', png: 'صورة', gif: 'صورة',
      webp: 'صورة', doc: 'Word', docx: 'Word', xls: 'Excel', xlsx: 'Excel',
      txt: 'نص', mp4: 'فيديو', mov: 'فيديو', avi: 'فيديو',
    };
    const label = typeMap[ext] || ext.toUpperCase();
    return `ملف.${label}`;
  }

  unblockUserInConversation(blockedUserId: number): void {
    const convId = this.activeConversationId();
    if (!convId) return;
    this.conversationService.unblockUser(convId, blockedUserId).subscribe({
      next: res => {
        if (res.isSuccess) {
          this.blockedByMe.update(s => { const n = new Set(s); n.delete(blockedUserId); return n; });
          if (this.blockedByMe().size === 0) this.isBlocked.set(false);
          this.showError('تم إلغاء الحظر بنجاح');
        } else {
          this.showError(res.message || 'حدث خطأ');
        }
      },
      error: (err: any) => this.showError(err?.error?.message || 'حدث خطأ'),
    });
  }

  private toChatConversation(dto: ConversationDto): ChatConversation {
    const currentUserId = this.authService.user()?.userId;
    const isGroup = dto.type === 'Group';
    let name: string;
    if (isGroup) {
      name = dto.title || 'مجموعة';
    } else {
      const other = dto.participants.find(p => p.userId !== currentUserId);
      name = other?.userName || dto.title || 'محادثة';
    }
    const myParticipant = dto.participants.find(p => p.userId === currentUserId);
    const lastReadAt = myParticipant?.lastReadAt ? new Date(myParticipant.lastReadAt).getTime() : 0;
    const lastMsgAt = dto.lastMessageAt ? new Date(dto.lastMessageAt).getTime() : 0;
    const unread = lastMsgAt > lastReadAt;
    return {
      id: dto.id,
      name,
      type: isGroup ? 'group' : 'individual',
      lastMessage: dto.lastMessage?.content ?? '',
      lastTime: this.formatTime(dto.lastMessageAt),
      unread,
      participants: dto.participants.map(p => ({ ...p, role: p.role ?? '' })),
      messages: [],
    };
  }

  private showError(message: string): void {
    this.errorMessage.set(message);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }

  getOtherParticipantRole(conv: ChatConversation): string {
    const currentUserId = this.authService.user()?.userId;
    const other = conv.participants.find(p => p.userId !== currentUserId);
    if (!other) return '';
    const roleMap: Record<string, string> = { 'Admin': 'مدير', 'Teacher': 'مدرس', 'Parent': 'ولي أمر', 'Student': 'طالب' };
    return roleMap[other.role] || other.role;
  }

  private getSenderRole(senderId: number, participants: ParticipantDto[]): string {
    return participants.find(p => p.userId === senderId)?.role ?? '';
  }

  private toChatMessage(dto: MessageDto, participants?: ParticipantDto[]): ChatMessage {
    const currentUserId = this.authService.user()?.userId;
    const role = participants ? this.getSenderRole(dto.senderId, participants) : '';
    return {
      id: dto.id,
      senderId: dto.senderId,
      senderName: dto.senderName,
      senderRole: role,
      content: dto.content,
      sentAt: dto.sentAt,
      isMine: dto.senderId === currentUserId,
      isEdited: dto.isEdited ?? false,
      attachmentUrl: dto.attachmentUrl ?? undefined,
      attachmentType: dto.attachmentType ?? undefined,
    };
  }

  private formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return date.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit' });
    if (days === 1) return 'أمس';
    if (days < 7) return `منذ ${days} أيام`;
    return date.toLocaleDateString('ar-EG', { month: 'short', day: 'numeric' });
  }
}
