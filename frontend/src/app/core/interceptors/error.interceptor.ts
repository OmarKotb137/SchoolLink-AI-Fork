import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { RoleService } from '../../shared/role.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);
  const router              = inject(Router);
  const auth         = inject(AuthService);
  const roleService         = inject(RoleService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      /*
       * الأولوية للرسالة القادمة من الـ backend (OperationResult.Message)
       * لأنها دايماً بالعربي ومحددة بالضبط لنوع الخطأ.
       * بنرجع لها فقط لو الـ status مش من الأنواع العامة اللي عندها رسالة ثابتة.
       */
      const backendMessage: string | undefined = error.error?.message;

      let message: string;

      if (error.status === 0) {
        message = 'لا يوجد اتصال بالإنترنت';
      } else if (error.status === 401) {
        message = 'انتهت الجلسة، يرجى تسجيل الدخول مجدداً';
        authService.logout();
        router.navigate([roleService.getLoginRoute()]);
      } else if (error.status === 403) {
        message = 'ليس لديك صلاحية للوصول لهذا المحتوى';
      } else if (error.status === 404) {
        message = backendMessage || 'العنصر المطلوب غير موجود';
      } else if (error.status >= 400 && error.status < 500) {
        message = backendMessage || 'خطأ في البيانات، يرجى المراجعة والمحاولة مرة أخرى';
      } else if (error.status >= 500) {
        message = 'خطأ في الخادم، يرجى المحاولة لاحقاً';
      } else {
        message = backendMessage || 'حدث خطأ غير متوقع';
      }
      return throwError(() => error);
    })
  );
};
