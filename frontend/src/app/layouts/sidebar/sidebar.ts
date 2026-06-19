import { Component, inject, input, output, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ROLE_MENUS, SidebarMenuItem, SidebarMenuSection } from '../../shared/menus';
import { RoleService } from '../../shared/role.service';
export type { SidebarMenuItem, SidebarMenuSection };

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  menuItems = input<SidebarMenuSection[]>([]);
  isOpen = input(false);
  isOpenChange = output<boolean>();

  private roleService = inject(RoleService);
  private authService = inject(AuthService);
  router = inject(Router);

  sections = computed(() => {
    const r = this.roleService.currentRole();
    if (r && ROLE_MENUS[r]) return ROLE_MENUS[r];
    return this.menuItems();
  });

  trackBySection(_: number, section: SidebarMenuSection) {
    return section.title;
  }

  trackByItem(_: number, item: SidebarMenuItem) {
    return item.route;
  }

  toggle() {
    this.isOpenChange.emit(!this.isOpen());
  }

  close() {
    this.isOpenChange.emit(false);
  }

  navigate(route: string) {
    this.close();
    this.router.navigate([route]);
  }

  logout() {
    this.close();
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate([this.roleService.getLoginRoute()]);
      }
    });
  }

  currentRole = computed(() => this.roleService.currentRole());

  onAiConsult() {
    this.navigate('/chat-ai');
  }
}
