import { Component, input, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService } from '../../shared/role.service';
import { ROLE_MENUS } from '../../shared/menus';

@Component({
  selector: 'app-topbar',
  imports: [],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css'
})
export class Topbar {
  private roleService = inject(RoleService);
  router = inject(Router);
  userName = input('أحمد');
  showSearch = input(true);
  showNotifications = input(true);
  showSpark = input(true);
  showSettings = input(true);
  showAvatar = input(true);
  notificationCount = input(0);

  userRole = computed(() => {
    const roleLabels: Record<string, string> = {
      admin: 'مدير النظام',
      teacher: 'مدرس',
      student: 'طالب',
      parent: 'ولي أمر',
    };
    return roleLabels[this.roleService.currentRole()] ?? '';
  });

  navItems = computed(() => {
    const items = ROLE_MENUS[this.roleService.currentRole()] ?? [];
    const homeItem = items[0];
    const chatItem = items.find(i => i.icon === 'chat' || i.label.includes('المحادثات'));
    const notifItem = items.find(i => i.icon === 'notifications');
    return { home: homeItem, chat: chatItem, notif: notifItem };
  });

  navigate(route: string) {
    this.router.navigate([route]);
  }
}
