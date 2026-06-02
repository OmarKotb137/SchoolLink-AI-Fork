import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-reports',
  imports: [Sidebar, Topbar],
  templateUrl: './reports.html',
  styleUrl: './reports.css',
})
export class Reports {
  sidebarOpen = signal(false);
}


