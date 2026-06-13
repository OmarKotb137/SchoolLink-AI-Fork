import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login-parent',
  imports: [FormsModule],
  templateUrl: './login-parent.html',
  styleUrl: './login-parent.css'
})
export class LoginParent {
  private auth = inject(AuthService);
  private router = inject(Router);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }
  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البيانات'); return; }
    const { username, password } = f.value;
    this.auth.login('parent', username, password).subscribe({
      next: () => this.router.navigate(['/parent']),
      error: (err) => alert(err.message)
    });
  }
}
