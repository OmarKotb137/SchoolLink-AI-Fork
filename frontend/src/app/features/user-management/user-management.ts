import { Component, signal, computed } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

interface Teacher {
  id: number;
  name: string;
  email: string;
  subjects: string[];
  classes: string[];
}

interface Student {
  id: number;
  name: string;
  class: string;
  guardianName: string;
  guardianPhone: string;
}

@Component({
  selector: 'app-user-management',
  imports: [Sidebar, Topbar],
  templateUrl: './user-management.html',
  styleUrl: './user-management.css'
})
export class UserManagement {
  sidebarOpen = signal(false);
  activeTab = signal<'teachers' | 'students'>('teachers');
  showAddModal = signal(false);
  editingTeacherId = signal<number | null>(null);
  editingStudentId = signal<number | null>(null);
  classFilter = signal<string>('all');

  newTeacherName = signal('');
  newTeacherEmail = signal('');
  newTeacherPassword = signal('');
  newTeacherSubjects = signal<string[]>([]);
  newTeacherClasses = signal<string[]>([]);

  newStudentName = signal('');
  newStudentClass = signal('');
  newGuardianName = signal('');
  newGuardianPhone = signal('');

  allSubjects = ['الرياضيات', 'اللغة العربية', 'اللغة الإنجليزية', 'العلوم', 'الدراسات الاجتماعية', 'التربية الدينية'];
  allClasses = ['الصف الأول - أ', 'الصف الأول - ب', 'الصف الثاني - أ', 'الصف الثاني - ب', 'الصف الثالث - أ', 'الصف الثالث - ب'];

  teachers = signal<Teacher[]>([
    { id: 1, name: 'أ. سارة أحمد', email: 'sara.ahmed@school.com', subjects: ['الرياضيات', 'العلوم'], classes: ['الصف الثالث - أ', 'الصف الثالث - ب'] },
    { id: 2, name: 'أ. محمد علي', email: 'mohamed.ali@school.com', subjects: ['اللغة العربية'], classes: ['الصف الثالث - أ', 'الصف الثاني - أ'] },
    { id: 3, name: 'أ. فاطمة حسن', email: 'fatma.hassan@school.com', subjects: ['اللغة الإنجليزية'], classes: ['الصف الثالث - ب', 'الصف الثاني - ب'] },
    { id: 4, name: 'م. عمر الحسيني', email: 'omar.husseini@school.com', subjects: ['الرياضيات', 'الدراسات الاجتماعية'], classes: ['الصف الثاني - أ', 'الصف الثاني - ب'] },
  ]);

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
    const f = this.classFilter();
    if (f === 'all') return this.students();
    return this.students().filter(s => s.class === f);
  });

  studentClasses = computed(() => [...new Set(this.students().map(s => s.class))]);

  toggleSubject(subj: string) {
    this.newTeacherSubjects.update(list => list.includes(subj) ? list.filter(s => s !== subj) : [...list, subj]);
  }

  toggleClass(cls: string) {
    this.newTeacherClasses.update(list => list.includes(cls) ? list.filter(c => c !== cls) : [...list, cls]);
  }

  openAddTeacher() {
    this.editingTeacherId.set(null);
    this.newTeacherName.set(''); this.newTeacherEmail.set(''); this.newTeacherPassword.set('');
    this.newTeacherSubjects.set([]); this.newTeacherClasses.set([]);
    this.showAddModal.set(true);
  }

  openEditTeacher(t: Teacher) {
    this.editingTeacherId.set(t.id);
    this.newTeacherName.set(t.name); this.newTeacherEmail.set(t.email);
    this.newTeacherSubjects.set([...t.subjects]); this.newTeacherClasses.set([...t.classes]);
    this.showAddModal.set(true);
  }

  openAddStudent() {
    this.editingStudentId.set(null);
    this.newStudentName.set(''); this.newStudentClass.set(''); this.newGuardianName.set(''); this.newGuardianPhone.set('');
    this.showAddModal.set(true);
  }

  openEditStudent(s: Student) {
    this.editingStudentId.set(s.id);
    this.newStudentName.set(s.name); this.newStudentClass.set(s.class);
    this.newGuardianName.set(s.guardianName); this.newGuardianPhone.set(s.guardianPhone);
    this.showAddModal.set(true);
  }

  closeModal() {
    this.showAddModal.set(false);
    this.editingTeacherId.set(null);
    this.editingStudentId.set(null);
  }

  saveTeacher() {
    const editId = this.editingTeacherId();
    if (editId) {
      this.teachers.update(list => list.map(t => t.id === editId ? { ...t, name: this.newTeacherName(), email: this.newTeacherEmail(), subjects: this.newTeacherSubjects(), classes: this.newTeacherClasses() } : t));
    } else {
      this.teachers.update(list => [...list, { id: Date.now(), name: this.newTeacherName(), email: this.newTeacherEmail(), password: this.newTeacherPassword(), subjects: this.newTeacherSubjects(), classes: this.newTeacherClasses() }]);
    }
    this.closeModal();
  }

  saveStudent() {
    const editId = this.editingStudentId();
    if (editId) {
      this.students.update(list => list.map(s => s.id === editId ? { ...s, name: this.newStudentName(), class: this.newStudentClass(), guardianName: this.newGuardianName(), guardianPhone: this.newGuardianPhone() } : s));
    } else {
      this.students.update(list => [...list, { id: Date.now(), name: this.newStudentName(), class: this.newStudentClass(), guardianName: this.newGuardianName(), guardianPhone: this.newGuardianPhone() }]);
    }
    this.closeModal();
  }

  deleteTeacher(id: number) {
    this.teachers.update(list => list.filter(t => t.id !== id));
  }

  deleteStudent(id: number) {
    this.students.update(list => list.filter(s => s.id !== id));
  }
}
