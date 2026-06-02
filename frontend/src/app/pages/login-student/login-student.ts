import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RoleService } from '../../shared/role.service';

@Component({
  selector: 'app-login-student',
  imports: [FormsModule],
  templateUrl: './login-student.html',
  styleUrl: './login-student.css'
})
export class LoginStudent {
  private roleService = inject(RoleService);

  togglePwd(pwd: HTMLInputElement) {
    pwd.type = pwd.type === 'password' ? 'text' : 'password';
  }
  handleLogin(f: any) {
    if (!f.valid) { alert('يرجى إدخال اسم المستخدم وكلمة المرور'); return; }
    this.roleService.setRole('student');
    window.location.href = '/student';
  }
}
