import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-chat-ai',
  imports: [Sidebar, Topbar],
  templateUrl: './chat-ai.html',
  styleUrl: './chat-ai.css',
})
export class ChatAi {
  sidebarOpen = signal(false);
}


