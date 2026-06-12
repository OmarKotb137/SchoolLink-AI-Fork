import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-staff',
  imports: [FormsModule],
  templateUrl: './login-staff.html',
})
export class LoginStaff {
  private auth = inject(AuthService);
  private router = inject(Router);
  private roleService = inject(RoleService);
  roleTab = signal<'admin' | 'teacher'>('admin');
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
