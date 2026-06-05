import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { RoomService, Room } from '../../core/services/room.service';

@Component({
  selector: 'app-room-management',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './room-management.html',
  styleUrl: './room-management.css',
})
export class RoomManagement implements OnInit {
  sidebarOpen = signal(false);

  private roomService = inject(RoomService);

  rooms = signal<Room[]>([]);
  roomTypes = [
    { value: 'Classroom',   label: 'قاعة دراسية' },
    { value: 'ScienceLab',  label: 'معمل علوم'   },
    { value: 'ComputerLab', label: 'معمل حاسب'   },
    { value: 'LanguageLab', label: 'معمل لغات'   },
    { value: 'Library',     label: 'مكتبة'        },
    { value: 'Playground',  label: 'ملعب'         },
    { value: 'Other',       label: 'أخرى'         }
  ];

  // plain property (not signal) — [(ngModel)] doesn't work with signals
  selectedTypeFilter = '';

  editingRoomId = signal<number | null>(null);

  errorMessage   = signal('');
  successMessage = signal('');

  newRoom: Partial<Room> = { name: '', capacity: 30, type: 'Classroom' };

  ngOnInit() {
    this.loadRooms();
  }

  loadRooms() {
    if (this.selectedTypeFilter) {
      this.roomService.getByType(this.selectedTypeFilter).subscribe({
        next: (data) => this.rooms.set(data),
        error: (err) => {
          console.error('Failed to load rooms by type', err);
          this.showError('فشل في تحميل القاعات. حاول مرة أخرى.');
        }
      });
    } else {
      this.roomService.getAll().subscribe({
        next: (data) => this.rooms.set(data),
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
    this.newRoom = { name: '', capacity: 30, type: 'Classroom' };
  }

  saveRoom() {
    if (!this.newRoom.name?.trim() || !this.newRoom.type) return;

    // Explicit cast — ngModel with type="number" can return a string
    const capacity = Number(this.newRoom.capacity);
    if (isNaN(capacity) || capacity < 1) {
      this.showError('السعة الاستيعابية يجب أن تكون رقماً أكبر من صفر.');
      return;
    }

    if (this.editingRoomId()) {
      const { name, type } = this.newRoom;
      // room.service.ts now injects `id` into the PUT body automatically
      this.roomService.update(this.editingRoomId()!, { name, capacity, type }).subscribe({
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
      this.roomService.create({ name: this.newRoom.name, capacity, type: this.newRoom.type }).subscribe({
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
    if (confirm('هل أنت متأكد من حذف هذه القاعة؟')) {
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
