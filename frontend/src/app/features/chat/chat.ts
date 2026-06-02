import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-chat',
  imports: [Sidebar, Topbar],
  templateUrl: './chat.html',
  styleUrl: './chat.css',
})
export class Chat {
  sidebarOpen = signal(false);
}
