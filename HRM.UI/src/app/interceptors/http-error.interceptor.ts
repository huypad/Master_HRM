import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const HttpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        router.navigate(['/error/401']);
      } else if (error.status === 403) {
        router.navigate(['/error/403']);
      } else if (error.status === 500) {
        router.navigate(['/error/500']);
      }

      return throwError(() => error);
    }),
  );
};
