import { Component, input, inject, computed, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { RoleService } from '../../shared/role.service';
import { ROLE_MENUS, SidebarMenuItem } from '../../shared/menus';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-topbar',
  imports: [],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css'
})
export class Topbar implements OnInit {
  private roleService = inject(RoleService);
  private notifService = inject(NotificationService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  router = inject(Router);
  userName = input('أحمد');
  showSearch = input(true);
  showNotifications = input(true);
  showSpark = input(true);
  showSettings = input(true);
  showAvatar = input(true);
  notificationCount = input(0);

  profilePictureUrl = signal<string | null>(null);
  isLoadingAvatar = signal(false);

  displayName = computed(() => this.authService.user()?.fullName || this.userName());
  displayAvatar = computed(() => this.authService.user()?.profilePictureUrl || this.profilePictureUrl());

  userRole = computed(() => {
    const roleLabels: Record<string, string> = {
      admin: 'مدير النظام',
      teacher: 'مدرس',
      student: 'طالب',
      parent: 'ولي أمر',
    };
    const role = this.roleService.currentRole();
    return role ? roleLabels[role] ?? '' : '';
  });

  navItems = computed(() => {
    const role = this.roleService.currentRole();
    const items: SidebarMenuItem[] = role ? (ROLE_MENUS[role] ?? []) : [];
    const homeItem = items[0];
    const chatItem = items.find((i: SidebarMenuItem) => i.icon === 'chat');
    const notifItem = items.find((i: SidebarMenuItem) => i.icon === 'notifications');
    return { home: homeItem, chat: chatItem, notif: notifItem };
  });

  realNotifCount = computed(() => {
    return this.notifService.unreadCount();
  });

  ngOnInit() {
    const user = this.authService.user();
    if (user) {
      this.notifService.getUnreadCount(user.userId).subscribe();
      this.loadProfilePicture();
    }
  }

  private loadProfilePicture() {
    const user = this.authService.user();
    if (!user) return;

    this.isLoadingAvatar.set(true);
    this.userService.getMyProfile().subscribe({
      next: res => {
        const data = res.data;
        this.profilePictureUrl.set(data?.profilePictureUrl ?? null);
        if (data?.profilePictureUrl || data?.fullName) {
          this.authService.updateUserInfo(
            data.fullName || user.fullName,
            data.profilePictureUrl
          );
        }
        this.isLoadingAvatar.set(false);
      },
      error: () => {
        this.profilePictureUrl.set(null);
        this.isLoadingAvatar.set(false);
      }
    });
  }

  navigate(route: string) {
    this.router.navigate([route]);
  }
}