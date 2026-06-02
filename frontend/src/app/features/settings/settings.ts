import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-settings',
  imports: [Sidebar, Topbar],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  sidebarOpen = signal(false);
}


