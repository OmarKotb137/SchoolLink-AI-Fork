import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-guardian',
  imports: [FormsModule],
  templateUrl: './login-guardian.html',
})
export class LoginGuardian {
  private auth = inject(AuthService);
  private router = inject(Router);
  roleTab = signal<'student' | 'parent'>('student');

  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البريد الإلكتروني وكلمة المرور'); return; }
    const { email, password } = f.value;
    this.auth.login(this.roleTab(), email, password).subscribe({
      next: () => this.router.navigate(['/' + this.roleTab()]),
      error: (err) => alert(err.message)
    });
  }
}
