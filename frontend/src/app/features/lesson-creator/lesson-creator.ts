import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
@Component({
  selector: 'app-lesson-creator',
  imports: [Sidebar],
  templateUrl: './lesson-creator.html',
  styleUrl: './lesson-creator.css'
})
export class LessonCreator {
  sidebarOpen = signal(false);
}
