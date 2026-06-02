import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NotificationService } from '../services/notification.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'An unexpected error occurred';

      if (error.status === 0) {
        message = 'No internet connection';
      } else if (error.status === 401) {
        message = 'Session expired. Please login again.';
        router.navigate(['/login']);
      } else if (error.status === 404) {
        message = 'Resource not found';
      } else if (error.status === 500) {
        message = 'Server error. Please try again later.';
      }

      notificationService.addMessage(message);
      return throwError(() => error);
    })
  );
};
