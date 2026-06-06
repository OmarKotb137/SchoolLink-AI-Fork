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

  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البريد الإلكتروني وكلمة المرور'); return; }
    const { email, password } = f.value;
    this.auth.login(this.roleTab(), email, password).subscribe({
      next: (session) => this.router.navigateByUrl(this.roleService.getHomeRoute(session.role)),
      error: (err) => alert(err.message)
    });
  }
}
