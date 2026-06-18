import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { Topbar } from './layouts/topbar/topbar';
import { AuthService } from './core/services/auth.service';
import { filter, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Topbar],
  templateUrl: './app.html',
})
export class App implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  private routerSub?: Subscription;
  currentUrl = signal('');

  ngOnInit() {
    this.currentUrl.set(this.router.url);
    this.routerSub = this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.currentUrl.set(this.router.url));
  }

  ngOnDestroy() {
    this.routerSub?.unsubscribe();
  }

  showTopbar = computed(() => {
    const url = this.currentUrl();
    return !!this.authService.token() &&
           !url.startsWith('/login') &&
           !url.startsWith('/index');
  });
}
