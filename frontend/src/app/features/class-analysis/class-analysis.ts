import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
@Component({
  selector: 'app-class-analysis',
  imports: [Sidebar],
  templateUrl: './class-analysis.html',
  styleUrl: './class-analysis.css'
})
export class ClassAnalysis {
  sidebarOpen = signal(false);
}
