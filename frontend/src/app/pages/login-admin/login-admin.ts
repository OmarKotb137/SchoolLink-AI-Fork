import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-admin',
  imports: [FormsModule],
  templateUrl: './login-admin.html',
  styleUrl: './login-admin.css'
})
export class LoginAdmin {
  private roleService = inject(RoleService);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }

  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البريد الإلكتروني وكلمة المرور'); return; }
    this.roleService.setRole('admin');
    window.location.href = '/admin';
  }
}
