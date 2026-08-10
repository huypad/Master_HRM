import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

// HTTP Interceptor tự động bắt và xử lý lỗi từ tất cả các API
export const ErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'Đã xảy ra lỗi kết nối với máy chủ.';

      if (error.error && typeof error.error === 'object' && error.error.message) {
        errorMessage = error.error.message;
      } else if (error.status === 0) {
        errorMessage = 'Không thể kết nối Backend (http://localhost:5177). Vui lòng kiểm tra lại dịch vụ Backend.';
      } else if (error.status >= 400 && error.status < 500) {
        errorMessage = `Yêu cầu không hợp lệ (${error.status}): ${error.statusText || 'Vui lòng kiểm tra lại thông tin'}`;
      } else if (error.status >= 500) {
        errorMessage = `Lỗi nội bộ hệ thống máy chủ (${error.status}). Vui lòng thử lại sau.`;
      }

      // Hiển thị Toast thông báo lỗi tới người dùng
      notificationService.showError(errorMessage);
      return throwError(() => error);
    })
  );
};
