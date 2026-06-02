import { Component, signal } from '@angular/core';
import { Sidebar } from '../../layouts/sidebar/sidebar';
import { Topbar } from '../../layouts/topbar/topbar';

@Component({
  selector: 'app-digital-library',
  imports: [Sidebar, Topbar],
  templateUrl: './digital-library.html',
  styleUrl: './digital-library.css',
})
export class DigitalLibrary {
  sidebarOpen = signal(false);
}
