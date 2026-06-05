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
        throw new HttpErrorResponse({
          error: body,
          status: 400,
          statusText: 'Bad Request',
          url: event.url ?? undefined,
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
        status: 0,
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
