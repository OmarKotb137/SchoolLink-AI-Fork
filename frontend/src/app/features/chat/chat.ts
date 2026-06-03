import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface Message {
  id: number;
  sender: string;
  senderRole: 'teacher' | 'parent' | 'admin' | 'student' | 'system';
  content: string;
  time: string;
  isMine: boolean;
  read: boolean;
  isAnnouncement?: boolean;
  attachment?: { name: string; size: string; type: string };
}

interface Conversation {
  id: number;
  name: string;
  type: 'individual' | 'group';
  subtype?: 'class' | 'subject' | 'general';
  lastMessage: string;
  lastTime: string;
  unread: number;
  online?: boolean;
  avatar?: string;
  initials?: string;
  members?: number;
  memberCount?: number;
  messages: Message[];
}

@Component({
  selector: 'app-chat',
  imports: [Sidebar, Topbar],
  templateUrl: './chat.html',
  styleUrl: './chat.css',
})
export class Chat {
  sidebarOpen = signal(false);
  activeFilter = signal<'all' | 'groups' | 'individual'>('all');
  activeConversationId = signal<number | null>(1);
  newMessage = signal('');

  conversations = signal<Conversation[]>([
    {
      id: 1, name: 'مجموعة الصف الخامس - أ', type: 'group', subtype: 'class',
      lastMessage: 'تم تحديث جدول الامتحانات الأسبوع المقبل', lastTime: '١٠:٤٥ ص',
      unread: 3, members: 35, memberCount: 35,
      initials: 'خ', messages: [
        { id: 1, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'السلام عليكم، تم تحديث جدول الامتحانات للصف الخامس', time: '١٠:٣٠ ص', isMine: false, read: true, isAnnouncement: true },
        { id: 2, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'يرجى الاطلاع على الجدول المرفق وإبلاغ الطلاب', time: '١٠:٣١ ص', isMine: false, read: true, attachment: { name: 'جدول_الامتحانات.pdf', size: '245 KB', type: 'pdf' } },
        { id: 3, sender: 'وليد محمد', senderRole: 'parent', content: 'تم الاستلام شكراً أستاذة', time: '١٠:٣٥ ص', isMine: false, read: true },
        { id: 4, sender: 'أحمد علي', senderRole: 'parent', content: 'هل هناك تعليمات محددة للامتحانات؟', time: '١٠:٣٨ ص', isMine: false, read: true },
        { id: 5, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'نعم، سيتم إرسال تعليمات منفصلة لكل مادة', time: '١٠:٤٠ ص', isMine: false, read: true },
        { id: 6, sender: 'أحمد علي', senderRole: 'parent', content: 'شكراً جزيلاً للتوضيح', time: '١٠:٤٢ ص', isMine: false, read: true },
        { id: 7, sender: 'خالد محمود', senderRole: 'parent', content: 'هل تم تحديد موعد الاجتماع الدوري؟', time: '١٠:٤٣ ص', isMine: false, read: false },
        { id: 8, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'الاجتماع القادم سيكون يوم الأحد المقبل الساعة ٦ مساءً', time: '١٠:٤٥ ص', isMine: false, read: false },
      ]
    },
    {
      id: 2, name: 'مجموعة الرياضيات', type: 'group', subtype: 'subject',
      lastMessage: 'نرجو مراجعة التغييرات الأخيرة في المنهج', lastTime: 'أمس',
      unread: 1, members: 22, memberCount: 22,
      initials: 'ر', messages: [
        { id: 1, sender: 'أ. علي النجار', senderRole: 'teacher', content: 'تم تحديث منهج الرياضيات للفصل الدراسي الثاني', time: 'أمس ٠٢:٠٠ م', isMine: false, read: true },
        { id: 2, sender: 'نظام SchoolLink', senderRole: 'system', content: 'تم إضافة ملف: تحديثات المنهج الجديد', time: 'أمس ٠٢:٠١ م', isMine: false, read: true, attachment: { name: 'تحديثات_المنهج.pdf', size: '1.2 MB', type: 'pdf' } },
        { id: 3, sender: 'م. عمر الحسيني', senderRole: 'teacher', content: 'نرجو مراجعة التغييرات الأخيرة في المنهج', time: 'أمس ٠٢:١٥ م', isMine: false, read: false },
      ]
    },
    {
      id: 3, name: 'أ. سارة أحمد', type: 'individual',
      lastMessage: 'تطور ملحوظ في أداء خالد هذا الشهر', lastTime: 'الآن',
      unread: 2, online: true,
      avatar: 'https://lh3.googleusercontent.com/aida-public/AB6AXuATHVLYmiPTjcJqiagRTAR0BiQ0huGxCPFs_1AXoljcLuoITT1ifMqyXQb-R8olC5-2koTzmOLx1a0yQp0hvHZ--YpWDA7lK3enwoiRe5JvKYC-q4qlkQIVBW6WLnjnEgsGsaiTYUI_2FqtrEBJ-lbpwTOtTQQi5vEux6Erls2O7SMP-y5CDr9aqRF7jl3jiJsyTWDpG9305IhtMXBYl5JpCfYQ32SeSBf7AyQZ72hULpQpMIWYGcvGCIuQ_l6m66sns6-boo9w-4g',
      messages: [
        { id: 1, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'السلام عليكم سيد محمد، أردت أن أشارككم خبراً سعيداً بخصوص أداء خالد', time: '٠٩:١٥ ص', isMine: false, read: true },
        { id: 2, sender: 'محمد', senderRole: 'parent', content: 'وعليكم السلام أ. سارة. يسعدني سماع ذلك! كيف كانت نتيجته؟', time: '٠٩:١٨ ص', isMine: true, read: true },
        { id: 3, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'لقد حصل على علامة كاملة في اختبار الرياضيات!', time: '٠٩:٢٠ ص', isMine: false, read: true },
        { id: 4, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'إليكم التقرير المفصل لأدائه', time: '٠٩:٢١ ص', isMine: false, read: true, attachment: { name: 'تقرير_خالد_الرياضيات.pdf', size: '1.2 MB', type: 'pdf' } },
        { id: 5, sender: 'محمد', senderRole: 'parent', content: 'شكراً جزيلاً أستاذة، كيف يمكننا دعم استمرارية هذا التفوق؟', time: '٠٩:٢٥ ص', isMine: true, read: true },
        { id: 6, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'أنصح بمتابعة حل الواجبات اليومية ومراجعة الدروس أول بأول', time: '٠٩:٢٨ ص', isMine: false, read: false },
        { id: 7, sender: 'أ. سارة أحمد', senderRole: 'teacher', content: 'تطور ملحوظ في أداء خالد هذا الشهر', time: '٠٩:٣٠ ص', isMine: false, read: false },
      ]
    },
    {
      id: 4, name: 'م. عمر الحسيني', type: 'individual',
      lastMessage: 'تم تحديث جدول الامتحانات النهائية', lastTime: '١٠:٤٥ ص',
      unread: 0, online: false,
      avatar: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCt_d_B8Wilw3Y6f1TmuNBFUAjnwU57qwXCRyPVCE8Zfu60IJk0zMRrEtTpZyAzNa3mm-WtjXfdADDIgJD52XOAzpNOXm_H_CMSmY6Ob_MjBKSTG0do2PNXkPCveD1kj51Q7-hpp7l0QK9beW_zCsG_sPjp9flRVkElteyzD1nY7-PT2_Z3T__f0iL4ynDQ1JKxp056tbpJkPqe9NNQjsrX44lWFwfClDaCvzrPfKRYEssChS8DArGxRKnOSdUFJKxvuprFBFhfjsc',
      messages: [
        { id: 1, sender: 'م. عمر الحسيني', senderRole: 'teacher', content: 'السلام عليكم، تم تحديث جدول الامتحانات النهائية', time: '١٠:٤٥ ص', isMine: false, read: true },
        { id: 2, sender: 'محمد', senderRole: 'parent', content: 'وعليكم السلام، هل هناك تغييرات عن الجدول السابق؟', time: '١٠:٤٨ ص', isMine: true, read: true },
      ]
    },
    {
      id: 5, name: 'لجنة المناهج', type: 'group', subtype: 'general',
      lastMessage: 'نرجو مراجعة التغييرات الأخيرة', lastTime: 'أمس',
      unread: 0, members: 15, memberCount: 15,
      initials: 'ل', messages: [
        { id: 1, sender: 'رئيس اللجنة', senderRole: 'admin', content: 'تم الانتهاء من مراجعة المناهج للفصل القادم', time: 'أمس ٠١:٠٠ م', isMine: false, read: true },
        { id: 2, sender: 'عضو اللجنة', senderRole: 'teacher', content: 'هل تم اعتماد التعديلات النهائية؟', time: 'أمس ٠١:٣٠ م', isMine: false, read: true },
      ]
    },
  ]);

  activeConversation = computed(() =>
    this.conversations().find(c => c.id === this.activeConversationId()) ?? null
  );

  filteredConversations = computed(() => {
    const filter = this.activeFilter();
    const all = this.conversations();
    if (filter === 'all') return all;
    if (filter === 'groups') return all.filter(c => c.type === 'group');
    return all.filter(c => c.type === 'individual');
  });

  smartSuggestions = signal([
    'شكراً جزيلاً، هل هناك أي تحديثات أخرى؟',
    'تم الاستلام، سنتابع مع الطالب',
    'هل تحتاج منا أي متابعة إضافية؟',
  ]);

  setActiveFilter(filter: 'all' | 'groups' | 'individual') {
    this.activeFilter.set(filter);
  }

  selectConversation(id: number) {
    this.activeConversationId.set(id);
    const conv = this.conversations().find(c => c.id === id);
    if (conv) {
      conv.unread = 0;
    }
  }

  sendMessage() {
    const text = this.newMessage().trim();
    if (!text) return;
    const conv = this.conversations().find(c => c.id === this.activeConversationId());
    if (!conv) return;
    conv.messages.push({
      id: conv.messages.length + 1,
      sender: 'محمد',
      senderRole: 'parent',
      content: text,
      time: 'الآن',
      isMine: true,
      read: true,
    });
    conv.lastMessage = text;
    conv.lastTime = 'الآن';
    this.newMessage.set('');
  }

  useSuggestion(text: string) {
    this.newMessage.set(text);
  }
}
