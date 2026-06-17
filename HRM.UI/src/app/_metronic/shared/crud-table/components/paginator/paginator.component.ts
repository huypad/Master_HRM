import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { PageSizes, PaginatorState } from '../../models/paginator.model';
import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NgPagination } from '../paginator/ng-pagination/ng-pagination.component';


@Component({
  selector: 'app-paginator',
  templateUrl: './paginator.component.html',
  styleUrls: ['./paginator.component.scss'],
  imports: [
    ReactiveFormsModule,
    FormsModule,
    CommonModule,
    ReactiveFormsModule, RouterModule, NgPagination,
  ],
  standalone: true,
})
export class PaginatorComponent implements OnInit {
  @Input() paginator!: PaginatorState;
  @Input() isLoading: any;
  @Output() paginate: EventEmitter<PaginatorState> = new EventEmitter();
  pageSizes: number[] = PageSizes;
  sodonghientai: number = 1;
  sodongcuoitrang: number = 1;
  constructor(
  ) {

  }

  ngOnInit(): void {
    this.paginator.pageSize = +this.paginator.pageSize || PageSizes[0]; // Default
    this.paginator.page = this.paginator.page || 1;

    this.updateRange();
    this.paginate.emit(this.paginator);
  }

  pageChange(num: number) {
    this.paginator.page = num;
    this.updateRange();
    this.paginate.emit(this.paginator);
  }

  sizeChange() {
    this.paginator.pageSize = +this.paginator.pageSize;
    this.paginator.page = 1;
    this.updateRange();
    this.paginate.emit(this.paginator);
  }

  private updateRange() {
    if (this.paginator.page > 1) {
      this.sodonghientai = this.paginator.pageSize * (this.paginator.page - 1) + 1;
    } else {
      this.sodonghientai = 1;
    }

    this.sodongcuoitrang = (this.sodonghientai - 1) + this.paginator.pageSize;

    // ✅ Nếu không dùng total, giới hạn = tổng item hiện có
    if (this.paginator.total && this.sodongcuoitrang > this.paginator.total) {
      this.sodongcuoitrang = this.paginator.total;
    }
  }
}