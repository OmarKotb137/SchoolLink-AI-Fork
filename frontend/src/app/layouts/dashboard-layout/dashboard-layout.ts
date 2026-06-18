import { Component, signal, input } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar, SidebarMenuSection } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

@Component({
  selector: 'app-dashboard-layout',
  imports: [RouterOutlet, Sidebar, Topbar],
  templateUrl: './dashboard-layout.html',
  styleUrl: './dashboard-layout.css'
})
export class DashboardLayout {
  menuItems = input<SidebarMenuSection[]>([]);
  sidebarOpen = signal(false);
}
