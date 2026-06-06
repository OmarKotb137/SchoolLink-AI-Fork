import { HttpErrorResponse, HttpEvent, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

interface OperationResultLike<T = unknown> {
  isSuccess: boolean;
  message?: string;
  data?: T;
}

/**
 * API Response Interceptor
 * ========================
 * الواجهة الخلفية (Backend) ترجع كل البيانات ملفوفة في كائن Result:
 * { isSuccess: true, message: "...", data: [...] }
 *
 * هذا الـ Interceptor يقوم تلقائياً بفك الغلاف واستخراج .data
 * حتى لا نحتاج لتعديل كل Service يدوياً.
 *
 * في حالة isSuccess = false يرمي HttpErrorResponse يحمل:
 *  - status : الـ HTTP status code الأصلي من الـ server (مش ثابت 400)
 *  - error  : كائن الـ OperationResult كاملاً بما فيه message العربي
 */
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    map((event: HttpEvent<unknown>) => {
      if (!(event instanceof HttpResponse)) {
        return event;
      }

      const body = event.body;
      if (!isOperationResultLike(body)) {
        return event;
      }

      if (!body.isSuccess) {
        // نحتفظ بـ HTTP status الأصلي — لو كان 200 بشكل غلط نرجع 400 كـ fallback
        throw new HttpErrorResponse({
          error:      body,
          status:     event.status >= 400 ? event.status : 400,
          statusText: event.statusText,
          url:        event.url ?? undefined,
        });
      }

      if (!('data' in body)) {
        return event.clone({ body: null });
      }

      return event.clone({ body: body.data });
    }),
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        return throwError(() => error);
      }

      return throwError(() => new HttpErrorResponse({
        error,
        status:     0,
        statusText: 'Client Error',
      }));
    })
  );
};

function isOperationResultLike(value: unknown): value is OperationResultLike {
  return !!value
    && typeof value === 'object'
    && 'isSuccess' in value
    && typeof (value as { isSuccess?: unknown }).isSuccess === 'boolean';
}
