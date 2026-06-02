import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-notifications',
  imports: [Sidebar, Topbar],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css',
})
export class Notifications {
  sidebarOpen = signal(false);
}


