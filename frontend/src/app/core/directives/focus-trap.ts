import {
  Directive,
  ElementRef,
  EventEmitter,
  HostListener,
  OnInit,
  OnDestroy,
  Output,
  inject,
} from '@angular/core';

/**
 * FocusTrap
 * =========
 * يحصر حركة الـ Tab داخل عنصر (Modal / Dialog) لتحسين accessibility،
 * ويرجّع التركيز للعنصر الأصلي عند الإغلاق، ويبعت حدث `escape` عند ضغط Esc.
 *
 * الاستخدام:
 *   <div appFocusTrap (escape)="close()">
 */
@Directive({
  selector: '[appFocusTrap]',
  standalone: true,
})
export class FocusTrap implements OnInit, OnDestroy {
  @Output('appFocusTrapEscape') escape = new EventEmitter<void>();

  private host = inject(ElementRef<HTMLElement>);
  private previouslyFocused: HTMLElement | null = null;

  private static readonly FOCUSABLE = [
    'a[href]',
    'button:not([disabled])',
    'textarea:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    '[tabindex]:not([tabindex="-1"])',
  ].join(',');

  ngOnInit(): void {
    this.previouslyFocused = (document.activeElement as HTMLElement) ?? null;
    // انتظر render العناصر الداخلية ثم ركّز على أول عنصر تفاعلي
    Promise.resolve().then(() => this.focusFirst());
  }

  ngOnDestroy(): void {
    this.previouslyFocused?.focus?.();
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'Tab':
        this.handleTab(event);
        break;
      case 'Escape':
        event.stopPropagation();
        this.escape.emit();
        break;
    }
  }

  private handleTab(event: KeyboardEvent): void {
    const focusables = this.getFocusables();
    if (!focusables.length) {
      event.preventDefault();
      return;
    }

    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    const active = document.activeElement as HTMLElement | null;
    const hostEl = this.host.nativeElement;

    if (event.shiftKey) {
      if (active === first || !hostEl.contains(active)) {
        event.preventDefault();
        last.focus();
      }
    } else {
      if (active === last || !hostEl.contains(active)) {
        event.preventDefault();
        first.focus();
      }
    }
  }

  /** يركّز على أول عنصر تفاعلي قابل للتركيز داخل الـ host */
  focusFirst(): void {
    const focusables = this.getFocusables();
    if (focusables.length) {
      focusables[0].focus();
    } else {
      this.host.nativeElement.focus();
    }
  }

  private getFocusables(): HTMLElement[] {
    const host = this.host.nativeElement as HTMLElement;
    return Array.from(
      host.querySelectorAll<HTMLElement>(FocusTrap.FOCUSABLE)
    ).filter(el => {
      // تجاهل العناصر المخفية فعليًا
      const style = window.getComputedStyle(el);
      return style.display !== 'none'
        && style.visibility !== 'hidden'
        && el.getClientRects().length > 0;
    });
  }
}
