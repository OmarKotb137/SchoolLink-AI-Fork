import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-reports-training',
  imports: [Sidebar, Topbar],
  templateUrl: './reports-training.html',
  styleUrl: './reports-training.css',
})
export class ReportsTraining {
  sidebarOpen = signal(false);
}


