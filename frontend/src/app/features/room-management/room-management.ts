import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { RoomService, Room } from '../../core/services/room.service';

@Component({
  selector: 'app-room-management',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar],
  templateUrl: './room-management.html',
  styleUrl: './room-management.css',
})
export class RoomManagement implements OnInit {
  sidebarOpen = signal(false);
  displayUserName = localStorage.getItem('fullName') || localStorage.getItem('username') || 'المشرف';

  private roomService = inject(RoomService);

  rooms = signal<Room[]>([]);

  currentPage = signal(1);
  itemsPerPage = signal(10);

  paginatedRooms = computed(() => {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.rooms().slice(start, start + this.itemsPerPage());
  });

  totalPages = computed(() => {
    return Math.max(1, Math.ceil(this.rooms().length / this.itemsPerPage()));
  });

  rangeStart = computed(() => {
    if (this.rooms().length === 0) return 0;
    return (this.currentPage() - 1) * this.itemsPerPage() + 1;
  });

  rangeEnd = computed(() => {
    return Math.min(this.currentPage() * this.itemsPerPage(), this.rooms().length);
  });

  pages = computed<(number | string)[]>(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const result: (number | string)[] = [];

    result.push(1);
    if (current > 3) result.push('...');
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      result.push(i);
    }
    if (current < total - 2) result.push('...');
    if (total > 1) result.push(total);

    return result;
  });

  trackByPageIndex = (_: number, item: number | string) =>
    typeof item === 'string' ? `dot-${_}` : `page-${item}`;

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
    }
  }

  prevPage() {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
    }
  }

  goToPage(page: number) {
    this.currentPage.set(page);
  }

  roomTypes = [
    { value: 'Classroom',   label: 'قاعة دراسية' },
    { value: 'ScienceLab',  label: 'معمل علوم'   },
    { value: 'ComputerLab', label: 'معمل حاسب'   },
    { value: 'LanguageLab', label: 'معمل لغات'   },
    { value: 'Library',     label: 'مكتبة'        },
    { value: 'Playground',  label: 'ملعب'         },
    { value: 'Other',       label: 'أخرى'         }
  ];

  otherRoomTypeValue = 'Other';
  selectedRoomType = 'Classroom';
  customRoomType = '';
  roomTypeOptions = computed(() => this.roomTypes);
  availableRoomTypes = computed(() => this.roomTypes);

  // plain property (not signal) — [(ngModel)] doesn't work with signals
  selectedTypeFilter = '';

  editingRoomId = signal<number | null>(null);

  errorMessage   = signal('');
  successMessage = signal('');
  deleteRoomConfirmId = signal<number | null>(null);

  newRoom: Partial<Room> = { name: '', capacity: 30, type: 'Classroom' };

  ngOnInit() {
    this.loadRooms();
  }

  loadRooms() {
    if (this.selectedTypeFilter) {
      this.roomService.getByType(this.selectedTypeFilter).subscribe({
        next: (data) => {
          this.rooms.set(data.data ?? data);
          this.currentPage.set(1);
        },
        error: (err) => {
          console.error('Failed to load rooms by type', err);
          this.showError('فشل في تحميل القاعات. حاول مرة أخرى.');
        }
      });
    } else {
      this.roomService.getAll().subscribe({
        next: (data) => {
          this.rooms.set(data.data ?? data);
          this.currentPage.set(1);
        },
        error: (err) => {
          console.error('Failed to load rooms', err);
          this.showError('فشل في تحميل القاعات. تأكد من الاتصال بالخادم.');
        }
      });
    }
  }

  onFilterChange() {
    this.loadRooms();
  }

  editRoom(room: Room) {
    this.editingRoomId.set(room.id);
    const hasKnownType = this.roomTypes.some(type => type.value === room.type);
    this.selectedRoomType = hasKnownType ? room.type : this.otherRoomTypeValue;
    this.customRoomType = hasKnownType ? '' : room.type;
    this.newRoom = {
      name:     room.name,
      // FIX: Backend Capacity is int? — null-coalesce to 30 so the number
      //      input always has a valid starting value and the save-validation
      //      (capacity < 1) never fires on an unedited room.
      capacity: room.capacity ?? 30,
      type:     room.type
    };
  }

  cancelEdit() {
    this.editingRoomId.set(null);
    this.selectedRoomType = 'Classroom';
    this.customRoomType = '';
    this.newRoom = { name: '', capacity: 30, type: 'Classroom' };
  }

  onRoomTypeChange() {
    if (this.selectedRoomType === this.otherRoomTypeValue) {
      this.newRoom.type = this.customRoomType.trim();
      return;
    }

    this.customRoomType = '';
    this.newRoom.type = this.selectedRoomType;
  }

  saveRoom() {
    const resolvedType = this.selectedRoomType === this.otherRoomTypeValue
      ? this.customRoomType.trim()
      : this.selectedRoomType;

    if (!this.newRoom.name?.trim() || !resolvedType) {
      this.showError('أدخل اسم القاعة ونوعها قبل الحفظ.');
      return;
    }

    // Explicit cast — ngModel with type="number" can return a string
    const capacity = Number(this.newRoom.capacity);
    if (isNaN(capacity) || capacity < 1) {
      this.showError('السعة الاستيعابية يجب أن تكون رقماً أكبر من صفر.');
      return;
    }

    if (this.editingRoomId()) {
      const { name } = this.newRoom;
      // room.service.ts now injects `id` into the PUT body automatically
      this.roomService.update(this.editingRoomId()!, { name, capacity, type: resolvedType }).subscribe({
        next: () => {
          this.loadRooms();
          this.cancelEdit();
          this.showSuccess('تم تحديث القاعة بنجاح!');
        },
        error: (err) => {
          console.error('Update failed', err);
          const msg = err.error?.message || err.error || 'فشل في تحديث القاعة. حاول مرة أخرى.';
          this.showError(msg);
        }
      });
    } else {
      this.roomService.create({ name: this.newRoom.name, capacity, type: resolvedType }).subscribe({
        next: () => {
          this.loadRooms();
          this.cancelEdit();
          this.showSuccess('تم إضافة القاعة بنجاح!');
        },
        error: (err) => {
          console.error('Create failed', err);
          const msg = err.error?.message || err.error || 'فشل في إضافة القاعة. حاول مرة أخرى.';
          this.showError(msg);
        }
      });
    }
  }

  deleteRoom(id: number) {
    this.deleteRoomConfirmId.set(id);
  }

  cancelDeleteRoom() {
    this.deleteRoomConfirmId.set(null);
  }

  confirmDeleteRoom() {
    const id = this.deleteRoomConfirmId();
    if (!id) return;
    this.deleteRoomConfirmId.set(null);
    this.roomService.delete(id).subscribe({
      next: () => {
        this.loadRooms();
        this.showSuccess('تم حذف القاعة بنجاح!');
      },
      error: (err) => {
        console.error('Delete failed', err);
        this.showError('فشل في حذف القاعة. حاول مرة أخرى.');
      }
    });
  }

  getRoomTypeLabel(value: string): string {
    return this.roomTypes.find(t => t.value === value)?.label ?? value;
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    this.successMessage.set('');
    setTimeout(() => this.errorMessage.set(''), 4000);
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    this.errorMessage.set('');
    setTimeout(() => this.successMessage.set(''), 3000);
  }
}
