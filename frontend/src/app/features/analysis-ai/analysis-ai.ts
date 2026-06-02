import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-analysis-ai',
  imports: [Sidebar, Topbar],
  templateUrl: './analysis-ai.html',
  styleUrl: './analysis-ai.css',
})
export class AnalysisAi {
  sidebarOpen = signal(false);
}


