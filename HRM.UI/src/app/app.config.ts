import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { LoadingInterceptor } from './interceptors/loading.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';
import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(
      // Đăng ký cả LoadingInterceptor và ErrorInterceptor cho mọi HTTP call toàn cục
      withInterceptors([LoadingInterceptor, ErrorInterceptor])
    )
  ]
};
