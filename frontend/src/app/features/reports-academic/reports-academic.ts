import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
@Component({
  selector: 'app-reports-academic',
  imports: [Sidebar],
  templateUrl: './reports-academic.html',
  styleUrl: './reports-academic.css',
})
export class ReportsAcademic {
  sidebarOpen = signal(false);
}


