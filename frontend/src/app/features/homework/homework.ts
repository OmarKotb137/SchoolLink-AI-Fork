import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-homework',
  imports: [Sidebar, Topbar],
  templateUrl: './homework.html',
  styleUrl: './homework.css'
})
export class Homework {
  sidebarOpen = signal(false);
}
