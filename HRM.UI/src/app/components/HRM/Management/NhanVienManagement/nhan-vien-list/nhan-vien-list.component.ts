import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { MatTableDataSource } from '@angular/material/table';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';

import { SortIconComponent } from '../../../../../_metronic/shared/crud-table/components/sort-icon/sort-icon.component';
import { SortDirection } from '../../../../../_metronic/shared/crud-table/sort.model';

import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';

import { PaginatorComponent } from '../../../../../_metronic/shared/crud-table/components/paginator/paginator.component';
import { PaginatorState } from '../../../../../_metronic/shared/crud-table/models/paginator.model';
import {
  LayoutUtilsService,
  MessageType,
} from '../../../_core/utils/layout-utils.service';

import { NhanVienDTO } from '../Model/nhan-vien.model';
import { NhanVienService } from '../Services/nhan-vien.service';
import { NhanVienEditDialogComponent } from '../nhan-vien-edit/nhan-vien-edit-dialog.component';

@Component({
  selector: 'app-nhan-vien-list',
  standalone: true,
  templateUrl: './nhan-vien-list.component.html',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatDialogModule,
    PaginatorComponent,
    SortIconComponent,
  ],
})
export class NhanVienListComponent implements OnInit, OnDestroy {
  displayedColumns: string[] = [];

  private readonly privateColumns: string[] = [
    'stt',
    'maNV',
    'hoTen',
    'ngaySinh',
    'cmnd',
    'mobile',
    'email',
    'thaotac',
  ];

  private readonly publicColumns: string[] = [
    'stt',
    'maNV',
    'hoTen',
    'ngaySinh',
    'cmnd',
    'mobile',
    'email',
  ];

  paginator = new PaginatorState();
  isLoading = false;

  accessMode: 'public' | 'private' = 'private';

  sorting: { column: string; direction: SortDirection } = {
    column: '',
    direction: '',
  };

  searchKeyword = '';

  dataSource = new MatTableDataSource<NhanVienDTO>();
  private subscriptions: Subscription[] = [];

  constructor(
    private dialog: MatDialog,
    private layoutUtilsService: LayoutUtilsService,
    public nhanVienService: NhanVienService
  ) {}

  ngOnInit(): void {
    this.subscriptions.push(
      this.nhanVienService.items$.subscribe((data) => {
        this.dataSource.data = data ?? [];
      }),

      this.nhanVienService.tableState$.subscribe((state) => {
        this.paginator.page = state.paginator.pageIndex + 1;
        this.paginator.pageSize = state.paginator.pageSize;
        this.paginator.total = state.paginator.total;

        this.searchKeyword = state.searchTerm ?? '';

        this.sorting = {
          column: state.sorting.column ?? '',
          direction: (state.sorting.direction ?? '') as SortDirection,
        };
      }),

      this.nhanVienService.isLoading$.subscribe((value) => {
        this.isLoading = value;
      })
    );

    this.accessMode = this.nhanVienService.getAccessMode();
    this.updateDisplayedColumns();
    this.loadFirstPage();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach((s) => s.unsubscribe());
  }

  changeAccessMode(mode: 'public' | 'private'): void {
    if (this.accessMode === mode) {
      return;
    }

    this.accessMode = mode;
    this.updateDisplayedColumns();
    this.nhanVienService.setAccessMode(mode);
  }

  create(): void {
    if (this.accessMode !== 'private') {
      return;
    }
    const dialogRef = this.dialog.open(NhanVienEditDialogComponent, {
      width: '800px',
      maxHeight: '90vh',
      disableClose: true,
      autoFocus: false,
      data: { item: { id_NV: 0 } },
    });

    dialogRef.afterClosed().subscribe((res) => {
      if (res?.status === 1) {
        this.reloadCurrentPage();
      }
    });
  }

  update(item: NhanVienDTO): void {
    if (this.accessMode !== 'private') {
      return;
    }
    this.nhanVienService.getById(item.id_NV).subscribe((detail) => {
      const dialogRef = this.dialog.open(NhanVienEditDialogComponent, {
        width: '800px',
        maxHeight: '90vh',
        disableClose: true,
        autoFocus: false,
        data: { item: detail },
      });

      dialogRef.afterClosed().subscribe((res) => {
        if (res?.status === 1) {
          this.reloadCurrentPage();
        }
      });
    });
  }

  delete(item: NhanVienDTO): void {
    if (this.accessMode !== 'private') {
      return;
    }
    const popup = this.layoutUtilsService.deleteElement(
      'Xác nhận xóa',
      `Bạn có chắc chắn muốn xóa nhân viên "${item.hoTen ?? item.maNV ?? ''}" không?`,
      'Đang xóa...',
      'Xóa'
    );

    popup.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }

      this.nhanVienService.delete(item.id_NV).subscribe(() => {
        this.layoutUtilsService.showActionNotification(
          'Xóa thành công',
          MessageType.Delete,
          3000,
          true,
          false
        );

        this.reloadCurrentPage();
      });
    });
  }

  sort(column: string): void {
    if (this.sorting.column === column) {
      this.sorting.direction =
        this.sorting.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.sorting.column = column;
      this.sorting.direction = 'asc';
    }

    this.nhanVienService.load(
      this.searchKeyword,
      1,
      this.paginator.pageSize || 10,
      this.sorting.column,
      this.sorting.direction
    );
  }

  onSearch(keyword: string): void {
    this.searchKeyword = (keyword || '').trim();

    this.nhanVienService.load(
      this.searchKeyword,
      1,
      this.paginator.pageSize || 10,
      this.sorting.column,
      this.sorting.direction
    );
  }

  paginate(event: PaginatorState): void {
    this.paginator = event;

    this.nhanVienService.load(
      this.searchKeyword,
      event.page || 1,
      event.pageSize || 10,
      this.sorting.column,
      this.sorting.direction
    );
  }

  private loadFirstPage(): void {
    this.nhanVienService.load(
      this.searchKeyword,
      1,
      this.paginator.pageSize || 10,
      this.sorting.column,
      this.sorting.direction
    );
  }

  private reloadCurrentPage(): void {
    this.nhanVienService.load(
      this.searchKeyword,
      this.paginator.page || 1,
      this.paginator.pageSize || 10,
      this.sorting.column,
      this.sorting.direction
    );
  }

  getHeight(): string {
    return window.innerHeight - 259 + 'px';
  }

  private updateDisplayedColumns(): void {
  this.displayedColumns =
    this.accessMode === 'private'
      ? this.privateColumns
      : this.publicColumns;
}
}