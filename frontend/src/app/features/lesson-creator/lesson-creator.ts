import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-lesson-creator',
  imports: [Sidebar, Topbar],
  templateUrl: './lesson-creator.html',
  styleUrl: './lesson-creator.css'
})
export class LessonCreator {
  sidebarOpen = signal(false);
}
