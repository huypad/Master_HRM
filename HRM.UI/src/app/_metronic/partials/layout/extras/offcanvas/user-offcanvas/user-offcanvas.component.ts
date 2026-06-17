import { LayoutUtilsService } from '../../../../../../components/HRM/_core/utils/layout-utils.service';
import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { Observable, of } from 'rxjs';
import { User, UserModel } from '../../../../../../components/HRM/Management/AccountManagement/Model/account-management.model';
import { AuthService } from '../../../../../../services/auth.service';
import { ChangePasswordEditDialogComponent } from '../change-password-dialog/change-password-dialog.component';
import { MessageType } from '../../../../../../components/HRM/_core/utils/layout-utils.service';
import { MatDialog } from '@angular/material/dialog';
import { AuthData } from '../../../../../../models/auth-data.model'
import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Router } from '@angular/router';
import { MatFormField } from '@angular/material/form-field';

@Component({
  selector: 'app-user-offcanvas',
  templateUrl: './user-offcanvas.component.html',
  styleUrls: ['./user-offcanvas.component.scss'],
  imports: [
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    ReactiveFormsModule, RouterModule,
  ],
})
export class UserOffcanvasComponent implements OnInit {
  extrasUserOffcanvasDirection = 'offcanvas-right';
  user: AuthData | null = null;

  constructor(
    private layout: LayoutService,
    private auth: AuthService,
    public dialog: MatDialog,
    private router: Router,
    public layoutUtilsService: LayoutUtilsService
  ) {
    this.user = this.auth.getAuthFromLocalStorage();
  }

  ngOnInit(): void {
    this.extrasUserOffcanvasDirection = `offcanvas-${this.layout.getProp('extras.user.offcanvas.direction')}`;
    const currentUser = this.auth.getUser();
    this.user = currentUser ? { user: currentUser, token: '' } : null;
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  closeOffcanvas(event: Event): void {
    event.preventDefault();
    const panel = document.getElementById('kt_quick_user');
    if (panel) {
      panel.classList.remove('offcanvas-on'); // class này Metronic dùng để hiển thị panel
    }
    const overlay = document.querySelector('.offcanvas-overlay');
    if (overlay) {
      overlay.remove();
    }
  }

  doimatkhau() {
    const user = this.auth.getUser();
    const username = user ? user.username : '';
    let saveMessage = 'Đổi mật khẩu thành công';
    const messageType = MessageType.Create;
    const dialogRef = this.dialog.open(ChangePasswordEditDialogComponent, {
      data: { username },
      width: '700px',
      panelClass: ['mat-dialog-popup', 'with40'],
      enterAnimationDuration: '0ms'
    });
    const sb = dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
      } else {
        this.layoutUtilsService.showActionNotification(saveMessage, messageType, 4000, true, false);
        this.logout();
      }
    });
  }
}
