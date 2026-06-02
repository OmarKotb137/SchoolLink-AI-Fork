import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-reports-academic',
  imports: [Sidebar, Topbar],
  templateUrl: './reports-academic.html',
  styleUrl: './reports-academic.css',
})
export class ReportsAcademic {
  sidebarOpen = signal(false);
}


