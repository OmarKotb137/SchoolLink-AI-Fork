import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-class-schedule',
  imports: [Sidebar, Topbar],
  templateUrl: './class-schedule.html',
  styleUrl: './class-schedule.css'
})
export class ClassSchedule {
  sidebarOpen = signal(false);
}
