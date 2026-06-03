import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface Teacher {
  id: number;
  name: string;
  email: string;
  password?: string;
  phone?: string;
  subjects: string[];
  classes: string[];
}

@Component({
  selector: 'app-add-teacher',
  imports: [Sidebar, Topbar],
  templateUrl: './add-teacher.html',
  styleUrl: './add-teacher.css'
})
export class AddTeacher {
  sidebarOpen = signal(false);
  showEditModal = signal(false);
  editingTeacherId = signal<number | null>(null);

  newName = signal('');
  newEmail = signal('');
  newPassword = signal('');
  newPhone = signal('');
  selectedSubjects = signal<string[]>([]);
  selectedClasses = signal<string[]>([]);

  allSubjects = ['الرياضيات', 'اللغة العربية', 'اللغة الإنجليزية', 'العلوم', 'الدراسات الاجتماعية', 'التربية الدينية'];
  allClasses = ['الصف الأول - أ', 'الصف الأول - ب', 'الصف الثاني - أ', 'الصف الثاني - ب', 'الصف الثالث - أ', 'الصف الثالث - ب'];

  teachers = signal<Teacher[]>([
    { id: 1, name: 'أ. سارة أحمد', email: 'sara.ahmed@school.com', phone: '01001234567', subjects: ['الرياضيات', 'العلوم'], classes: ['الصف الثالث - أ', 'الصف الثالث - ب'] },
    { id: 2, name: 'أ. محمد علي', email: 'mohamed.ali@school.com', phone: '01002345678', subjects: ['اللغة العربية'], classes: ['الصف الثالث - أ', 'الصف الثاني - أ'] },
    { id: 3, name: 'أ. فاطمة حسن', email: 'fatma.hassan@school.com', phone: '01003456789', subjects: ['اللغة الإنجليزية'], classes: ['الصف الثالث - ب', 'الصف الثاني - ب'] },
    { id: 4, name: 'م. عمر الحسيني', email: 'omar.husseini@school.com', phone: '01004567890', subjects: ['الرياضيات', 'الدراسات الاجتماعية'], classes: ['الصف الثاني - أ', 'الصف الثاني - ب'] },
  ]);

  toggleSubject(subj: string) {
    this.selectedSubjects.update(list => list.includes(subj) ? list.filter(s => s !== subj) : [...list, subj]);
  }

  toggleClass(cls: string) {
    this.selectedClasses.update(list => list.includes(cls) ? list.filter(c => c !== cls) : [...list, cls]);
  }

  resetForm() {
    this.newName.set(''); this.newEmail.set(''); this.newPassword.set(''); this.newPhone.set('');
    this.selectedSubjects.set([]); this.selectedClasses.set([]);
    this.editingTeacherId.set(null);
  }

  saveTeacher() {
    this.teachers.update(list => [...list, {
      id: Date.now(),
      name: this.newName(),
      email: this.newEmail(),
      password: this.newPassword() || undefined,
      phone: this.newPhone() || undefined,
      subjects: this.selectedSubjects(),
      classes: this.selectedClasses()
    }]);
    this.resetForm();
  }

  editTeacher(t: Teacher) {
    this.editingTeacherId.set(t.id);
    this.newName.set(t.name); this.newEmail.set(t.email); this.newPassword.set(''); this.newPhone.set(t.phone || '');
    this.selectedSubjects.set([...t.subjects]); this.selectedClasses.set([...t.classes]);
    this.showEditModal.set(true);
  }

  updateTeacher() {
    const id = this.editingTeacherId();
    if (id) {
      this.teachers.update(list => list.map(t => t.id === id ? {
        ...t,
        name: this.newName(),
        email: this.newEmail(),
        phone: this.newPhone() || undefined,
        subjects: this.selectedSubjects(),
        classes: this.selectedClasses()
      } : t));
    }
    this.closeEditModal();
  }

  closeEditModal() {
    this.showEditModal.set(false);
    this.editingTeacherId.set(null);
    this.resetForm();
  }

  deleteTeacher(id: number) {
    this.teachers.update(list => list.filter(t => t.id !== id));
  }
}
