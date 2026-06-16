import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
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
  private authService = inject(AuthService);

  @ViewChild('photoInput') photoInputRef!: ElementRef<HTMLInputElement>;

  // ─── Core ─────────────────────────────────────────────────────────────────
  sidebarOpen      = signal(false);
  profile          = signal<User | null>(null);
  isLoading        = signal(false);
  isEmailLoading   = signal(false);
  isPhotoLoading   = signal(false);
  errorMessage     = signal<string | null>(null);
  successMessage   = signal<string | null>(null);

  fullName = signal('');
  phone    = signal('');
  email    = signal('');
  otpCode  = signal('');

  // ─── Password ─────────────────────────────────────────────────────────────
  currentPassword  = signal('');
  newPassword      = signal('');
  confirmPassword  = signal('');
  showCurrentPwd   = signal(false);
  showNewPwd       = signal(false);
  showConfirmPwd   = signal(false);
  isPasswordLoading   = signal(false);
  pwdSubmitAttempted  = signal(false);

  /** يظهر Panel تغيير كلمة المرور للمعلمين وأولياء الأمور فقط */
  canChangePassword = computed(() => {
    const role = (this.profile()?.role ?? '').toLowerCase();
    return role === 'teacher' || role === 'parent';
  });

  /** قوة كلمة المرور الجديدة */
  newPasswordStrength = computed<'weak' | 'medium' | 'strong'>(() => {
    const pwd = this.newPassword();
    if (!pwd || pwd.length < 6) return 'weak';
    let score = 0;
    if (pwd.length >= 8)           score++;
    if (/[A-Z]/.test(pwd))         score++;
    if (/[a-z]/.test(pwd))         score++;
    if (/[0-9]/.test(pwd))         score++;
    if (/[^A-Za-z0-9]/.test(pwd))  score++;
    if (score >= 4) return 'strong';
    if (score >= 2) return 'medium';
    return 'weak';
  });

  /** أخطاء نموذج كلمة المرور */
  passwordErrors = computed(() => {
    const current = this.currentPassword();
    const newPwd  = this.newPassword();
    const confirm = this.confirmPassword();
    const errors: string[] = [];

    if (!current)
      errors.push('كلمة المرور الحالية مطلوبة');

    if (!newPwd || newPwd.length < 6)
      errors.push('كلمة المرور الجديدة 6 أحرف على الأقل');

    if (newPwd && current && newPwd === current)
      errors.push('كلمة المرور الجديدة يجب أن تختلف عن الحالية');

    if (newPwd && confirm && newPwd !== confirm)
      errors.push('كلمة المرور وتأكيدها غير متطابقتان');

    return errors;
  });

  /** هل النموذج صالح للإرسال */
  isPasswordFormValid = computed(() =>
    this.passwordErrors().length === 0 &&
    !!this.currentPassword() &&
    this.newPassword().length >= 6 &&
    this.newPassword() === this.confirmPassword()
  );

  // ─── Lifecycle ────────────────────────────────────────────────────────────
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

  // ─── Account Data ─────────────────────────────────────────────────────────
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

  // ─── Photo ────────────────────────────────────────────────────────────────
  triggerPhotoUpload() {
    this.photoInputRef.nativeElement.click();
  }

  onPhotoSelected(event: Event) {
    const input  = event.target as HTMLInputElement;
    const file   = input.files?.[0];
    const userId = this.profile()?.id;
    if (!file || !userId) return;

    this.isPhotoLoading.set(true);
    this.errorMessage.set(null);

    this.userService.uploadProfilePhoto(userId, file).subscribe({
      next: res => {
        this.profile.update(p => p ? { ...p, profilePictureUrl: res.photoUrl } : p);
        this.isPhotoLoading.set(false);
        this.showSuccess('تم تحديث الصورة الشخصية بنجاح');
        input.value = '';
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر رفع الصورة'));
        this.isPhotoLoading.set(false);
        input.value = '';
      }
    });
  }

  deletePhoto() {
    this.isPhotoLoading.set(true);
    this.errorMessage.set(null);

    this.userService.deleteProfilePhoto().subscribe({
      next: () => {
        this.profile.update(p => p ? { ...p, profilePictureUrl: undefined } : p);
        this.isPhotoLoading.set(false);
        this.showSuccess('تم حذف الصورة الشخصية');
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر حذف الصورة'));
        this.isPhotoLoading.set(false);
      }
    });
  }

  // ─── Email OTP ────────────────────────────────────────────────────────────
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

  // ─── Password Change ──────────────────────────────────────────────────────
  changePassword() {
    this.pwdSubmitAttempted.set(true);
    if (!this.isPasswordFormValid()) return;

    this.isPasswordLoading.set(true);
    this.errorMessage.set(null);

    this.authService.changePassword(
      this.currentPassword(),
      this.newPassword(),
      this.confirmPassword()
    ).subscribe({
      next: () => {
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        this.pwdSubmitAttempted.set(false);
        this.isPasswordLoading.set(false);
        this.showSuccess('تم تغيير كلمة المرور بنجاح');
      },
      error: err => {
        this.showError(this.extractErrorMessage(err, 'تعذر تغيير كلمة المرور، تأكد من صحة كلمة المرور الحالية'));
        this.isPasswordLoading.set(false);
      }
    });
  }

  // ─── Helpers ──────────────────────────────────────────────────────────────
  roleLabel(role?: string): string {
    switch ((role ?? '').toLowerCase()) {
      case 'admin':   return 'إدارة';
      case 'teacher': return 'معلم';
      case 'parent':  return 'ولي أمر';
      case 'student': return 'طالب';
      default:        return role ?? '';
    }
  }

  strengthLabel(s: 'weak' | 'medium' | 'strong'): string {
    return s === 'weak' ? 'ضعيفة' : s === 'medium' ? 'متوسطة' : 'قوية';
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
