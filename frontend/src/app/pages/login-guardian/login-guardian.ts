import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-guardian',
  imports: [FormsModule],
  templateUrl: './login-guardian.html',
})
export class LoginGuardian {
  private auth = inject(AuthService);
  private router = inject(Router);
  private roleService = inject(RoleService);
  roleTab = signal<'student' | 'parent'>('student');
  errorMsg = signal('');

  handleLogin(f: any) {
    this.errorMsg.set('');
    if (!f.valid) { this.errorMsg.set('يرجى إدخال اسم المستخدم وكلمة المرور'); return; }
    const { username, password } = f.value;
    this.auth.login(this.roleTab(), username, password).subscribe({
      next: (session) => this.router.navigateByUrl(this.roleService.getHomeRoute(session.role)),
      error: (err) => this.errorMsg.set(err.message || 'اسم المستخدم أو كلمة المرور غير صحيحة')
    });
  }
}
