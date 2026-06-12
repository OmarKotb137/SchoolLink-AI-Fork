import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';
import { User, UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, Sidebar, Topbar],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  private userService = inject(UserService);

  sidebarOpen = signal(false);
  profile = signal<User | null>(null);
  isLoading = signal(false);
  isEmailLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  fullName = signal('');
  phone = signal('');
  email = signal('');
  otpCode = signal('');

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.isLoading.set(true);
    this.userService.getMyProfile().subscribe({
      next: res => {
        const user = res.data ?? null;
        this.profile.set(user);
        this.fullName.set(user?.fullName ?? '');
        this.phone.set(user?.phone ?? '');
        this.email.set(user?.contactEmail ?? '');
        this.isLoading.set(false);
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحميل الملف الشخصي'));
        this.isLoading.set(false);
      }
    });
  }

  saveProfile() {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.userService.updateMyProfile({
      fullName: this.fullName(),
      phone: this.phone() || undefined
    }).subscribe({
      next: () => {
        this.showSuccess('تم تحديث بيانات الحساب بنجاح');
        this.loadProfile();
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تحديث بيانات الحساب'));
        this.isLoading.set(false);
      }
    });
  }

  sendOtp() {
    this.errorMessage.set(null);
    this.isEmailLoading.set(true);
    this.userService.sendEmailOtp(this.email()).subscribe({
      next: () => {
        this.isEmailLoading.set(false);
        this.showSuccess('تم إرسال كود التفعيل إلى البريد الإلكتروني');
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر إرسال كود التفعيل'));
        this.isEmailLoading.set(false);
      }
    });
  }

  verifyOtp() {
    this.errorMessage.set(null);
    this.isEmailLoading.set(true);
    this.userService.verifyEmailOtp(this.email(), this.otpCode()).subscribe({
      next: () => {
        this.otpCode.set('');
        this.isEmailLoading.set(false);
        this.showSuccess('تم تفعيل البريد الإلكتروني بنجاح');
        this.loadProfile();
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تفعيل البريد الإلكتروني'));
        this.isEmailLoading.set(false);
      }
    });
  }

  roleLabel(role?: string): string {
    switch ((role ?? '').toLowerCase()) {
      case 'admin':
        return 'إدارة';
      case 'teacher':
        return 'معلم';
      case 'parent':
        return 'ولي أمر';
      case 'student':
        return 'طالب';
      default:
        return role ?? '';
    }
  }

  private showSuccess(message: string) {
    this.successMessage.set(message);
    setTimeout(() => this.successMessage.set(null), 3500);
  }

  private showError(message: string) {
    this.errorMessage.set(message);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    const e = err as { error?: { message?: string }; message?: string };
    return e?.error?.message || e?.message || fallback;
  }
}
