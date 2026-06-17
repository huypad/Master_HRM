import { GeneralService } from '../../../../../../components/HRM/_core/services/general.service';
import { ChangeDetectionStrategy, Component, ElementRef, Inject, OnDestroy, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { BehaviorSubject, Subscription } from 'rxjs';
import { LayoutUtilsService, MessageType } from '../../../../../../components/HRM/_core/utils/layout-utils.service';
import { ChangePasswordModel } from '../../../../../../components/HRM/_core/models/danhmuc.model';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-change-password-dialog',
  templateUrl: './change-password-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, MatProgressSpinnerModule,
    ReactiveFormsModule, MatDialogModule, MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
}) export class ChangePasswordEditDialogComponent implements OnInit, OnDestroy {
  itemForm!: FormGroup;
  isLoadingSubmit$ = new BehaviorSubject<boolean>(false);
  private subscriptions: Subscription[] = [];

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: any,
    public dialogRef: MatDialogRef<ChangePasswordEditDialogComponent>,
    private fb: FormBuilder,
    private service: GeneralService,
    private layoutUtilsService: LayoutUtilsService
  ) { }

  ngOnInit(): void {
    this.itemForm = this.fb.group(
      {
        PassWordOld: ['', Validators.required],
        PassWordNew: ['', [Validators.required, Validators.minLength(6)]],
        NhapLaiMatKhau: ['', Validators.required],
      },
      { validators: this.checkPasswords }
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((sb) => sb.unsubscribe());
  }

  checkPasswords(group: FormGroup) {
    const pass = group.get('PassWordNew')?.value;
    const confirm = group.get('NhapLaiMatKhau')?.value;
    return pass === confirm ? null : { notSame: true };
  }

  onSubmit() {
    if (this.itemForm.invalid) {
      this.validateAllFormFields(this.itemForm);
      return;
    }

    const model = new ChangePasswordModel();
    model.Username = this.data.username;
    model.PasswordOld = this.itemForm.get('PassWordOld')?.value;
    model.PasswordNew = this.itemForm.get('PassWordNew')?.value;

    this.isLoadingSubmit$.next(true);
    const sub = this.service.changePassword(model).subscribe({
      next: (res) => {
        this.isLoadingSubmit$.next(false);
        this.dialogRef.close(res);
      },
      error: (err) => {
        this.isLoadingSubmit$.next(false);
        this.layoutUtilsService.showError(err.error.message);
      },
    });
    this.subscriptions.push(sub);
  }

  validateAllFormFields(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach((field) => {
      const control = formGroup.get(field);
      if (control instanceof FormControl) {
        control.markAsTouched({ onlySelf: true });
      } else if (control instanceof FormGroup) {
        this.validateAllFormFields(control);
      }
    });
  }

  goBack() {
    this.dialogRef.close();
  }
}