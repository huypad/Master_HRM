import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  constructor(private snackBar: MatSnackBar) {}

  // Hiển thị thông báo thành công (Success Toast)
  showSuccess(message: string): void {
    this.snackBar.open(message, 'Đóng', {
      duration: 4000,
      panelClass: ['snackbar-success'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }

  // Hiển thị thông báo lỗi (Error Toast) cho người dùng
  showError(message: string): void {
    this.snackBar.open(message, 'Đóng', {
      duration: 6000,
      panelClass: ['snackbar-error'],
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }
}
