import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
@Component({
  selector: 'app-monthly-report',
  imports: [Sidebar],
  templateUrl: './monthly-report.html',
  styleUrl: './monthly-report.css',
})
export class MonthlyReport {
  sidebarOpen = signal(false);
}


