import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  HostListener,
  Inject,
  OnDestroy,
  OnInit,
  ViewEncapsulation,
} from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import {
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { BehaviorSubject, Subscription, finalize } from 'rxjs';
import { CommonModule } from '@angular/common';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIcon } from '@angular/material/icon';
import { ReactiveFormsModule } from '@angular/forms';

import {
  LayoutUtilsService,
  MessageType,
} from '../../../_core/utils/layout-utils.service';
import { NhanVienService } from '../Services/nhan-vien.service';
import { NhanVienDTO, NhanVienModel } from '../Model/nhan-vien.model';

@Component({
  selector: 'app-nhan-vien-edit-dialog',
  templateUrl: './nhan-vien-edit-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  standalone: true,
  imports: [
    CommonModule,
    MatIcon,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
  ],
})
export class NhanVienEditDialogComponent implements OnInit, OnDestroy {
  item: NhanVienDTO;
  formGroup!: FormGroup;

  isLoadingSubmit$ = new BehaviorSubject<boolean>(false);
  
  private subscriptions: Subscription[] = [];

  constructor(
    private fb: FormBuilder,
    private nhanVienService: NhanVienService,
    private layoutUtilsService: LayoutUtilsService,
    private dialogRef: MatDialogRef<NhanVienEditDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { item: NhanVienDTO }
  ) {
    this.item = data.item;
  }

  ngOnInit(): void {
    this.createForm();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((s) => s.unsubscribe());
    this.isLoadingSubmit$.complete();
  }

  getTitle(): string {
    return this.item?.id_NV && this.item.id_NV !== 0
      ? 'Cập nhật nhân viên'
      : 'Thêm nhân viên';
  }

  createForm(): void {
    const nameParts = this.splitHoTen(this.item?.hoTen);

    this.formGroup = this.fb.group({
      maNV: [
        this.item?.maNV ?? '',
        Validators.compose([Validators.required]),
      ],
      holot: [
        nameParts.holot,
        Validators.compose([Validators.required]),
      ],
      ten: [
        nameParts.ten,
        Validators.compose([Validators.required]),
      ],
      ngaySinh: [
        this.toDateInputValue(this.item?.ngaySinh),
      ],
      cmnd: [
        this.item?.cmnd && this.item.cmnd !== '****'
          ? this.item.cmnd
          : '',
      ],
      mobile: [
        this.item?.mobile ?? '',
      ],
      email: [
        this.item?.email ?? '',
        Validators.compose([Validators.email]),
      ],
      sotaikhoan: [
        this.item?.sotaikhoan ?? '',
      ],
    });
  }

  isControlInvalid(controlName: string, validationType: string): boolean {
    const control = this.formGroup.controls[controlName];

    if (!control) {
      return false;
    }

    return (
      control.hasError(validationType) &&
      (control.dirty || control.touched)
    );
  }

  onSaveAndContinue(): void {
    if (!this.prepareSave()) {
      return;
    }

    const model = this.prepareModel();

    this.isLoadingSubmit$.next(true);

    const sub = this.nhanVienService.create(model).subscribe({
      next: () => {
        this.isLoadingSubmit$.next(false);

        this.layoutUtilsService.showActionNotification(
          'Thao tác thành công',
          MessageType.Create,
          3000,
          true,
          false
        );

        this.formGroup.reset({
          maNV: '',
          holot: '',
          ten: '',
          ngaySinh: '',
          cmnd: '',
          mobile: '',
          email: '',
          sotaikhoan: '',
        });

        this.formGroup.markAsPristine();
        this.formGroup.markAsUntouched();
      },
      error: (err) => {
        console.error('Create nhan vien error:', err);
        this.isLoadingSubmit$.next(false);

        this.layoutUtilsService.showActionNotification(
          'Thao tác thất bại',
          MessageType.Delete,
          3000,
          true,
          false
        );
      },
    });

    this.subscriptions.push(sub);
  }

  onSaveAndClose(): void {
    if (!this.prepareSave()) {
      return;
    }

    const model = this.prepareModel();

    this.isLoadingSubmit$.next(true);

    if (!this.item?.id_NV || this.item.id_NV === 0) {
      const sub = this.nhanVienService.create(model).subscribe({
        next: () => {
          this.isLoadingSubmit$.next(false);

          this.layoutUtilsService.showActionNotification(
            'Thao tác thành công',
            MessageType.Create,
            3000,
            true,
            false
          );

          this.dialogRef.close({ status: 1 });
        },
        error: (err) => {
          console.error('Create nhan vien error:', err);
          this.isLoadingSubmit$.next(false);

          this.layoutUtilsService.showActionNotification(
            'Thao tác thất bại',
            MessageType.Delete,
            3000,
            true,
            false
          );
        },
      });

      this.subscriptions.push(sub);
      return;
    }

    const sub = this.nhanVienService.update(this.item.id_NV, model).subscribe({
      next: () => {
        this.isLoadingSubmit$.next(false);

        this.layoutUtilsService.showActionNotification(
          'Thao tác thành công',
          MessageType.Update,
          3000,
          true,
          false
        );

        this.dialogRef.close({ status: 1 });
      },
      error: (err) => {
        console.error('Update nhan vien error:', err);
        this.isLoadingSubmit$.next(false);

        this.layoutUtilsService.showActionNotification(
          'Thao tác thất bại',
          MessageType.Delete,
          3000,
          true,
          false
        );
      },
    });

    this.subscriptions.push(sub);
  }

  goBack(): void {
    this.dialogRef.close();
  }

  private prepareSave(): boolean {
    this.formGroup.markAllAsTouched();

    if (this.formGroup.invalid) {
      return false;
    }

    return true;
  }

  private prepareModel(): NhanVienModel {
    const value = this.formGroup.value;

    return {
      maNV: this.normalizeString(value.maNV),
      holot: this.normalizeString(value.holot),
      ten: this.normalizeString(value.ten),
      ngaySinh: value.ngaySinh || null,
      cmnd: this.normalizeString(value.cmnd),
      mobile: this.normalizeString(value.mobile),
      email: this.normalizeString(value.email),
      sotaikhoan: this.normalizeString(value.sotaikhoan),
    };
  }

  private normalizeString(value: any): string | undefined {
    if (value === null || value === undefined) {
      return undefined;
    }

    const text = value.toString().trim();

    return text.length > 0 ? text : undefined;
  }

  private toDateInputValue(value?: string | Date | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private splitHoTen(hoTen?: string | null): { holot: string; ten: string } {
    if (!hoTen || hoTen === '****') {
      return {
        holot: '',
        ten: '',
      };
    }

    const parts = hoTen.trim().split(/\s+/);

    if (parts.length === 0) {
      return {
        holot: '',
        ten: '',
      };
    }

    if (parts.length === 1) {
      return {
        holot: '',
        ten: parts[0],
      };
    }

    const ten = parts[parts.length - 1];
    const holot = parts.slice(0, parts.length - 1).join(' ');

    return {
      holot,
      ten,
    };
  }
}