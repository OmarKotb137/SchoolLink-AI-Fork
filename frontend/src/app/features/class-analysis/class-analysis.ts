import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-class-analysis',
  imports: [Sidebar, Topbar],
  templateUrl: './class-analysis.html',
  styleUrl: './class-analysis.css'
})
export class ClassAnalysis {
  sidebarOpen = signal(false);
}
