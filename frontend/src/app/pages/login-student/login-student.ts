import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-student',
  imports: [FormsModule],
  templateUrl: './login-student.html',
  styleUrl: './login-student.css'
})
export class LoginStudent {
  private roleService = inject(RoleService);
  private router = inject(Router);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }
  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال اسم المستخدم وكلمة المرور'); return; }
    this.roleService.setRole('student');
    this.router.navigate(['/student']);
  }
}
