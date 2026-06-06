import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-admin',
  imports: [FormsModule],
  templateUrl: './login-admin.html',
  styleUrl: './login-admin.css'
})
export class LoginAdmin {
  private roleService = inject(RoleService);
  private router = inject(Router);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }

  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البريد الإلكتروني وكلمة المرور'); return; }
    this.roleService.setRole('admin');
    this.router.navigate(['/admin']);
  }
}
