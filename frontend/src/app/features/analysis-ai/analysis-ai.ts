import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
@Component({
  selector: 'app-analysis-ai',
  imports: [Sidebar],
  templateUrl: './analysis-ai.html',
  styleUrl: './analysis-ai.css',
})
export class AnalysisAi {
  sidebarOpen = signal(false);
}


