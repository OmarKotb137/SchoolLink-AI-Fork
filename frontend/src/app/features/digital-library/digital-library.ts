import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { LibraryService } from '../../core/services/library.service';
import { LibraryItemDto, LibraryItemType } from '../../core/models/library.model';

@Component({
  selector: 'app-digital-library',
  standalone: true,
  imports: [Sidebar, Topbar, CommonModule, FormsModule],
  templateUrl: './digital-library.html',
  styleUrl: './digital-library.css',
})
export class DigitalLibrary implements OnInit {
  sidebarOpen = signal(false);
  private libraryService = inject(LibraryService);
  private sanitizer = inject(DomSanitizer);

  items = signal<LibraryItemDto[]>([]);
  latestItems = signal<LibraryItemDto[]>([]);
  subjects = signal<any[]>([]);
  
  loading = signal(false);
  loadingMore = signal(false);
  searchTerm = signal('');
  selectedSubjectId = signal<number | null>(null);
  currentPage = signal(1);
  pageSize = 12;
  hasMore = signal(false);

  showUploadModal = signal(false);
  uploading = signal(false);

  showViewerModal = signal(false);
  viewerItem = signal<LibraryItemDto | null>(null);
  viewerSafeUrl = signal<SafeResourceUrl | null>(null);

  inputMode = signal<'file' | 'link'>('file');
  deletingId = signal<number | null>(null);
  confirmDeleteId = signal<number | null>(null);

  toastMessage = signal('');
  toastType = signal<'success' | 'error'>('success');

  uploadForm = {
    title: '',
    description: '',
    itemType: LibraryItemType.Book,
    subjectId: null as number | null,
    file: null as File | null,
    linkUrl: ''
  };

  LibraryItemType = LibraryItemType;

  ngOnInit() {
    this.loadSubjects();
    this.loadLatest();
    this.loadItems();
  }

  loadSubjects() {
    this.libraryService.getSubjects().subscribe({
      next: (res) => {
        if (Array.isArray(res)) {
          this.subjects.set(res);
        }
      },
      error: (err) => console.error('Error fetching subjects', err)
    });
  }

  loadLatest() {
    this.libraryService.getLatest(1).subscribe({
      next: (res) => {
        this.latestItems.set(Array.isArray(res) ? res : []);
      },
      error: (err) => console.error('Error fetching latest items', err)
    });
  }

  loadItems() {
    this.loading.set(true);
    this.currentPage.set(1);
    this.libraryService.getAll({
      page: 1,
      pageSize: this.pageSize,
      subjectId: this.selectedSubjectId(),
      searchTerm: this.searchTerm() || null
    }).subscribe({
      next: (res) => {
        this.items.set(res.items ?? []);
        this.hasMore.set((res.items?.length ?? 0) >= this.pageSize);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error fetching library items', err);
        this.loading.set(false);
      }
    });
  }

  loadMore() {
    if (this.loadingMore() || !this.hasMore()) return;
    const nextPage = this.currentPage() + 1;
    this.loadingMore.set(true);
    this.libraryService.getAll({
      page: nextPage,
      pageSize: this.pageSize,
      subjectId: this.selectedSubjectId(),
      searchTerm: this.searchTerm() || null
    }).subscribe({
      next: (res) => {
        this.items.update(prev => [...prev, ...(res.items ?? [])]);
        this.currentPage.set(nextPage);
        this.hasMore.set((res.items?.length ?? 0) >= this.pageSize);
        this.loadingMore.set(false);
      },
      error: () => {
        this.loadingMore.set(false);
      }
    });
  }

  filterBySubject(subjectId: number | null) {
    this.selectedSubjectId.set(subjectId);
    this.loadItems();
  }

  onSearch() {
    this.loadItems();
  }

  getViewUrl(url: string | undefined): string {
    if (!url) return '#';
    return url.replace('dl.dropboxusercontent.com', 'www.dropbox.com').replace('?dl=1', '?dl=0');
  }

  getDownloadUrl(url: string | undefined): string {
    if (!url) return '#';
    return url.replace('www.dropbox.com', 'dl.dropboxusercontent.com').replace('?dl=0', '?dl=1');
  }

  getDirectUrl(url: string | undefined): string {
    if (!url) return '';
    return url.replace('www.dropbox.com', 'dl.dropboxusercontent.com').replace('?dl=0', '?raw=1').replace('?dl=1', '?raw=1');
  }

  getViewerIframeUrl(url: string | undefined): string {
    if (!url) return '#';
    if (this.isYouTubeLink(url)) {
      return this.getYTEmbedUrl(url);
    }
    const directUrl = this.getDownloadUrl(url); // Docs viewer needs download link
    return `https://docs.google.com/gview?url=${encodeURIComponent(directUrl)}&embedded=true`;
  }

  isImage(url: string | undefined): boolean {
    if (!url) return false;
    return /\.(jpg|jpeg|png|gif|webp)(\?.*)?$/i.test(url.toLowerCase());
  }

  getYTVideoId(url: string | undefined): string | null {
    if (!url) return null;
    const match = url.match(/(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([^&?]+)/);
    return match ? match[1] : null;
  }

  isYouTubeLink(url: string | undefined): boolean {
    return !!this.getYTVideoId(url);
  }

  getYTThumbnail(url: string | undefined): string | null {
    const id = this.getYTVideoId(url);
    // hqdefault (480x360) is ALWAYS available for every public YouTube video
    return id ? `https://img.youtube.com/vi/${id}/hqdefault.jpg` : null;
  }

  getYTEmbedUrl(url: string | undefined): string {
    const id = this.getYTVideoId(url);
    return id ? `https://www.youtube.com/embed/${id}` : '';
  }

  getSafeUrl(url: string): SafeResourceUrl {
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  openViewer(item: LibraryItemDto) {
    this.viewerItem.set(item);
    if (this.isYouTubeLink(item.fileUrl)) {
      this.viewerSafeUrl.set(this.getSafeUrl(this.getYTEmbedUrl(item.fileUrl)));
    } else if (item.itemType !== LibraryItemType.Video && !this.isImage(item.fileUrl)) {
      this.viewerSafeUrl.set(this.getSafeUrl(this.getViewerIframeUrl(item.fileUrl)));
    } else {
      this.viewerSafeUrl.set(null);
    }
    this.showViewerModal.set(true);
  }

  closeViewer() {
    this.showViewerModal.set(false);
    this.viewerItem.set(null);
    this.viewerSafeUrl.set(null);
  }

  showToast(message: string, type: 'success' | 'error' = 'success') {
    this.toastMessage.set(message);
    this.toastType.set(type);
    setTimeout(() => this.toastMessage.set(''), 4000);
  }

  onFileSelected(event: any) {
    if (event.target.files.length > 0) {
      this.uploadForm.file = event.target.files[0];
    }
  }

  openUploadModal() {
    console.log("Opening upload modal...");
    this.uploadForm = {
      title: '',
      description: '',
      itemType: LibraryItemType.Book,
      subjectId: null,
      file: null,
      linkUrl: ''
    };
    this.showUploadModal.set(true);
  }

  closeUploadModal() {
    this.showUploadModal.set(false);
  }

  submitUpload() {
    const isFileMode = this.inputMode() === 'file';
    const isLinkMode = this.inputMode() === 'link';
    const hasFile = !!this.uploadForm.file;
    const hasLink = !!this.uploadForm.linkUrl;

    if (!this.uploadForm.title || (isFileMode && !hasFile) || (isLinkMode && !hasLink)) return;
    
    this.uploading.set(true);
    this.libraryService.upload(
      isFileMode ? this.uploadForm.file : null,
      isLinkMode ? this.uploadForm.linkUrl : null,
      this.uploadForm.title,
      this.uploadForm.itemType,
      this.uploadForm.subjectId ?? null,
      null, // gradeLevelId
      null, // academicYearId
      this.uploadForm.description
    ).subscribe({
      next: () => {
        this.closeUploadModal();
        this.loadItems();
        this.loadLatest();
        this.showToast('تم رفع الملف بنجاح!', 'success');
        this.uploading.set(false);
      },
      error: (err) => {
        console.error('Upload error', err);
        let errorMsg = 'حدث خطأ في الاتصال بالخادم';
        if (err.error && err.error.error) {
          errorMsg = err.error.error;
        } else if (err.error && err.error.message) {
          errorMsg = err.error.message;
        }
        this.showToast('فشل الرفع: ' + errorMsg, 'error');
        this.uploading.set(false);
      }
    });
  }

  requestDelete(id: number) {
    this.confirmDeleteId.set(id);
  }

  cancelDelete() {
    this.confirmDeleteId.set(null);
  }

  confirmDelete() {
    const id = this.confirmDeleteId();
    if (!id) return;
    this.deletingId.set(id);
    this.confirmDeleteId.set(null);
    this.libraryService.delete(id).subscribe({
      next: () => {
        this.items.update(list => list.filter(i => i.id !== id));
        this.latestItems.update(list => list.filter(i => i.id !== id));
        this.showToast('تم حذف الملف بنجاح', 'success');
        this.deletingId.set(null);
      },
      error: () => {
        this.showToast('فشل حذف الملف، حاول مجدداً', 'error');
        this.deletingId.set(null);
      }
    });
  }
}
