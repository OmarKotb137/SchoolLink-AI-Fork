import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-teacher',
  imports: [FormsModule],
  templateUrl: './login-teacher.html',
  styleUrl: './login-teacher.css'
})
export class LoginTeacher {
  private auth = inject(AuthService);
  private router = inject(Router);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }
  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال اسم المستخدم وكلمة المرور'); return; }
    const { username, password } = f.value;
    this.auth.login('teacher', username, password).subscribe({
      next: () => this.router.navigate(['/teacher']),
      error: (err) => alert(err.message)
    });
  }
}
