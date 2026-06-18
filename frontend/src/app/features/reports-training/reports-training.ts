import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
@Component({
  selector: 'app-reports-training',
  imports: [Sidebar],
  templateUrl: './reports-training.html',
  styleUrl: './reports-training.css',
})
export class ReportsTraining {
  sidebarOpen = signal(false);
}


