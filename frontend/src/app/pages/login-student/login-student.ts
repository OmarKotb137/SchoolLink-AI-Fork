import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-student',
  imports: [FormsModule],
  templateUrl: './login-student.html',
  styleUrl: './login-student.css'
})
export class LoginStudent {
  private auth = inject(AuthService);
  private router = inject(Router);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }
  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال اسم المستخدم وكلمة المرور'); return; }
    const { email, password } = f.value;
    this.auth.login('student', email, password).subscribe({
      next: () => this.router.navigate(['/student']),
      error: (err) => alert(err.message)
    });
  }
}
