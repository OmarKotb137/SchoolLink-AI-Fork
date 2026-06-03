import { Component, inject, input, output, computed } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { ROLE_MENUS, SidebarMenuItem } from '../../shared/menus';
import { RoleService } from '../../shared/role.service';
export type { SidebarMenuItem };

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css'
})
export class Sidebar {
  menuItems = input<SidebarMenuItem[]>([]);
  isOpen = input(false);
  isOpenChange = output<boolean>();

  private roleService = inject(RoleService);
  router = inject(Router);

  items = computed(() => {
    const r = this.roleService.currentRole();
    if (r && ROLE_MENUS[r]) return ROLE_MENUS[r];
    return this.menuItems();
  });

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

  currentRole = computed(() => this.roleService.currentRole());

  onAiConsult() {
    alert('AI consultation coming soon!');
  }
}
