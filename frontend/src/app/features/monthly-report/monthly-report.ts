import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-monthly-report',
  imports: [Sidebar, Topbar],
  templateUrl: './monthly-report.html',
  styleUrl: './monthly-report.css',
})
export class MonthlyReport {
  sidebarOpen = signal(false);
}


