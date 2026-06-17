import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
// import { TranslationService } from 'src/app/modules/i18n/translation.service'; 
import { PageSizes, PaginatorState } from '../../models/paginator.model';
import { NgPagination } from './ng-pagination/ng-pagination.component'; // đúng path
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-paginator',
  templateUrl: './paginator.component.html',
  styleUrls: ['./paginator.component.scss'],
  imports: [CommonModule, FormsModule, NgPagination],
  standalone: true,
})
export class PaginatorComponent implements OnInit {
  @Input() paginator!: PaginatorState;
  @Input() isLoading: boolean = false;
  @Output() paginate: EventEmitter<PaginatorState> = new EventEmitter();
  @Input() collectionSize: number = 0;
@Input() page: number = 1;
@Input() maxSize: number = 4;
@Input() rotate: boolean = true;
@Input() boundaryLinks: boolean = true;
@Input() pageSize: number = 10;

  pageSizes: number[] = PageSizes;
  sodonghientai: number = 1;
  sodongcuoitrang: number = 1;

  constructor(
    // public translationService: TranslationService, 
  ) {}

  ngOnInit(): void {
    // ...giữ nguyên code tính toán, nhưng đảm bảo paginator luôn có giá trị trước khi dùng!
    if (!this.paginator) return;
    this.sodonghientai = this.sodongcuoitrang = 0;
    this.paginator.pageSize = +this.paginator.pageSize;
    this.paginator.page = 1;
    this.paginate.emit(this.paginator);
    if (this.paginator.page > 1)
      this.sodonghientai = this.paginator.pageSize * (this.paginator.page - 1) + 1;
    else
      this.sodonghientai = 1;
    this.sodongcuoitrang = (this.sodonghientai - 1) + this.paginator.pageSize;
    if (this.sodongcuoitrang > this.paginator.total)
      this.sodongcuoitrang = this.paginator.total;
    this.sodongcuoitrang = this.paginator.pageSize;
  }
  
  pageChange(num: number) {
    this.sodonghientai = this.sodongcuoitrang = 0;
    this.paginator.page = num;
    this.paginate.emit(this.paginator);
    if (this.paginator.page > 1)
      this.sodonghientai = this.paginator.pageSize * (this.paginator.page - 1) + 1;
    else
      this.sodonghientai = 1;
    this.sodongcuoitrang = (this.sodonghientai - 1) + this.paginator.pageSize;
    if (this.sodongcuoitrang > this.paginator.total)
      this.sodongcuoitrang = this.paginator.total;
  }

  sizeChange() {
    this.sodonghientai = this.sodongcuoitrang = 0;
    this.paginator.pageSize = +this.paginator.pageSize;
    this.paginator.page = 1;
    this.paginate.emit(this.paginator);
    if (this.paginator.page > 1)
      this.sodonghientai = this.paginator.pageSize * (this.paginator.page - 1) + 1;
    else
      this.sodonghientai = 1;
    this.sodongcuoitrang = (this.sodonghientai - 1) + this.paginator.pageSize;
    if (this.sodongcuoitrang > this.paginator.total)
      this.sodongcuoitrang = this.paginator.total;
  }
}
