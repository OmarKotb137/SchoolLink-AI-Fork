import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-parent',
  imports: [FormsModule],
  templateUrl: './login-parent.html',
  styleUrl: './login-parent.css'
})
export class LoginParent {
  private roleService = inject(RoleService);
  private router = inject(Router);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }
  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البيانات'); return; }
    this.roleService.setRole('parent');
    this.router.navigate(['/parent']);
  }
}
