import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface ImportedStudent {
  id: number;
  name: string;
}

@Component({
  selector: 'app-import-students',
  imports: [Sidebar, Topbar],
  templateUrl: './import-students.html',
  styleUrl: './import-students.css'
})
export class ImportStudents {
  sidebarOpen = signal(false);
  students = signal<ImportedStudent[]>([]);
  selectedClass = signal('');
  isDragging = signal(false);
  showSuccess = signal(false);
  showError = signal(false);

  allClasses = ['الصف الأول - أ', 'الصف الأول - ب', 'الصف الثاني - أ', 'الصف الثاني - ب', 'الصف الثالث - أ', 'الصف الثالث - ب'];

  onDragOver(e: DragEvent) {
    e.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(e: DragEvent) {
    e.preventDefault();
    this.isDragging.set(false);
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.isDragging.set(false);
    this.parseFile(e.dataTransfer?.files);
  }

  onFileSelected(e: Event) {
    const input = e.target as HTMLInputElement;
    this.parseFile(input.files);
    input.value = '';
  }

  parseFile(files: FileList | null | undefined) {
    if (!files || files.length === 0) return;
    this.showSuccess.set(false);
    this.showError.set(false);
    this.students.set([
      { id: 1, name: 'أحمد محمد السيد' },
      { id: 2, name: 'سارة علي حسن' },
      { id: 3, name: 'محمود حسن إبراهيم' },
      { id: 4, name: 'فاطمة أحمد عمر' },
      { id: 5, name: 'عمر سعيد محمود' },
      { id: 6, name: 'ليلى خالد محمد' },
      { id: 7, name: 'يوسف عبد الله ناصر' },
      { id: 8, name: 'مريم طارق حسين' },
      { id: 9, name: 'إبراهيم أحمد سامي' },
      { id: 10, name: 'نور حسن علي' },
    ]);
  }

  removeStudent(id: number) {
    this.students.update(list => list.filter(s => s.id !== id));
  }

  updateStudentName(id: number, name: string) {
    this.students.update(list => list.map(s => s.id === id ? { ...s, name } : s));
  }

  saveStudents() {
    if (!this.selectedClass()) {
      this.showError.set(true);
      return;
    }
    this.showSuccess.set(true);
    this.showError.set(false);
    this.students.set([]);
    this.selectedClass.set('');
  }

  resetAll() {
    this.students.set([]);
    this.selectedClass.set('');
    this.showSuccess.set(false);
    this.showError.set(false);
  }
}
