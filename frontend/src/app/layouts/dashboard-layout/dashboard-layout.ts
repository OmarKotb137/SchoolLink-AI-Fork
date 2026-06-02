import { Component, signal, input } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar, SidebarMenuItem } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

@Component({
  selector: 'app-dashboard-layout',
  imports: [RouterOutlet, Sidebar, Topbar],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.css'
})
export class DashboardLayout {
  menuItems = input<SidebarMenuItem[]>([]);
  sidebarOpen = signal(false);
}
