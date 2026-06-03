import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface Student {
  id: number;
  name: string;
  class: string;
  guardianName: string;
  guardianPhone?: string;
}

interface TransferRecord {
  id: number;
  studentName: string;
  fromClass: string;
  toClass: string;
  date: string;
  reason?: string;
}

@Component({
  selector: 'app-transfer-student',
  imports: [Sidebar, Topbar],
  templateUrl: './transfer-student.html',
  styleUrl: './transfer-student.css'
})
export class TransferStudent {
  sidebarOpen = signal(false);
  searchQuery = signal('');
  selectedStudent = signal<Student | null>(null);
  targetClass = signal('');
  transferReason = signal('');
  showError = signal(false);
  showSuccess = signal(false);
  lastTransfer = signal<{ fromClass: string; toClass: string } | null>(null);

  allClasses = ['الصف الأول - أ', 'الصف الأول - ب', 'الصف الثاني - أ', 'الصف الثاني - ب', 'الصف الثالث - أ', 'الصف الثالث - ب'];

  students = signal<Student[]>([
    { id: 1, name: 'أحمد محمود', class: 'الصف الثالث - أ', guardianName: 'محمود أحمد', guardianPhone: '01001234567' },
    { id: 2, name: 'سارة علي', class: 'الصف الثالث - أ', guardianName: 'علي حسن', guardianPhone: '01002345678' },
    { id: 3, name: 'محمود حسن', class: 'الصف الثالث - ب', guardianName: 'حسن محمود', guardianPhone: '01003456789' },
    { id: 4, name: 'فاطمة أحمد', class: 'الصف الثالث - أ', guardianName: 'أحمد عمر', guardianPhone: '01004567890' },
    { id: 5, name: 'عمر سعيد', class: 'الصف الثاني - أ', guardianName: 'سعيد علي', guardianPhone: '01005678901' },
    { id: 6, name: 'ليلى خالد', class: 'الصف الثالث - ب', guardianName: 'خالد محمد', guardianPhone: '01006789012' },
    { id: 7, name: 'خالد محمود', class: 'الصف الثاني - ب', guardianName: 'محمود سامي', guardianPhone: '01007890123' },
  ]);

  filteredStudents = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return this.students();
    return this.students().filter(s => s.name.includes(q));
  });

  availableClasses = computed(() => {
    const current = this.selectedStudent()?.class;
    return this.allClasses.filter(c => c !== current);
  });

  transferHistory = signal<TransferRecord[]>([
    { id: 1, studentName: 'أحمد محمود', fromClass: 'الصف الثاني - أ', toClass: 'الصف الثالث - أ', date: '2026-02-15', reason: 'الانتقال للصف الأعلى' },
    { id: 2, studentName: 'ليلى خالد', fromClass: 'الصف الثاني - ب', toClass: 'الصف الثالث - ب', date: '2026-02-15', reason: 'الانتقال للصف الأعلى' },
  ]);

  selectStudent(s: Student) {
    this.selectedStudent.set(s);
    this.targetClass.set('');
    this.transferReason.set('');
    this.showError.set(false);
  }

  cancelTransfer() {
    this.selectedStudent.set(null);
    this.targetClass.set('');
    this.transferReason.set('');
    this.showError.set(false);
  }

  confirmTransfer() {
    const student = this.selectedStudent();
    const target = this.targetClass();

    if (!student || !target) {
      this.showError.set(true);
      return;
    }

    this.showError.set(false);
    const fromClass = student.class;

    this.students.update(list => list.map(s =>
      s.id === student.id ? { ...s, class: target } : s
    ));

    this.transferHistory.update(list => [{
      id: Date.now(),
      studentName: student.name,
      fromClass,
      toClass: target,
      date: new Date().toISOString().split('T')[0],
      reason: this.transferReason() || undefined
    }, ...list]);

    this.lastTransfer.set({ fromClass, toClass: target });
    this.showSuccess.set(true);
    this.selectedStudent.set(null);
    this.targetClass.set('');
    this.transferReason.set('');

    setTimeout(() => {
      this.showSuccess.set(false);
      this.lastTransfer.set(null);
    }, 3000);
  }
}
