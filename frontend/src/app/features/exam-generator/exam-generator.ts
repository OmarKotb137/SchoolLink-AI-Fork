import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-exam-generator',
  imports: [Sidebar, Topbar],
  templateUrl: './exam-generator.html',
  styleUrl: './exam-generator.css'
})
export class ExamGenerator {
  sidebarOpen = signal(false);
}
