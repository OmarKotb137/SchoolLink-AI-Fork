import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-staff',
  imports: [FormsModule],
  templateUrl: './login-staff.html',
})
export class LoginStaff {
  private roleService = inject(RoleService);
  private router = inject(Router);
  roleTab = signal<'admin' | 'teacher'>('admin');

  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال البريد الإلكتروني وكلمة المرور'); return; }
    this.roleService.setRole(this.roleTab());
    this.router.navigate(['/' + this.roleTab()]);
  }
}
