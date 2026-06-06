import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor }    from './core/interceptors/auth.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { errorInterceptor }   from './core/interceptors/error.interceptor';

import { routes } from './app.routes';

/*
 * ترتيب الـ Interceptors مهم جداً:
 *
 * REQUEST  (↓) : auth → loading → error → api → server
 * RESPONSE (↑) : api → error → loading → auth
 *
 * 1. auth    : يضيف الـ Bearer token على كل طلب
 * 2. loading : يُظهر/يُخفي الـ spinner لكل طلب
 * 3. error   : يعترض أخطاء HTTP ويعرضها للمستخدم
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor, loadingInterceptor, errorInterceptor])
    ),
  ]
};
