import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-admin',
  imports: [FormsModule],
  templateUrl: './login-admin.html',
  styleUrl: './login-admin.css'
})
export class LoginAdmin {
  private auth = inject(AuthService);
  private router = inject(Router);
  errorMsg = signal('');

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }

  handleLogin(f: any) {
    this.errorMsg.set('');
    if (!f.valid) { this.errorMsg.set('يرجى إدخال اسم المستخدم وكلمة المرور'); return; }
    const { username, password } = f.value;
    this.auth.login('admin', username, password).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: (err) => this.errorMsg.set(err.message || 'اسم المستخدم أو كلمة المرور غير صحيحة')
    });
  }
}
