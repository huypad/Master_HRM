import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginatorComponent } from './components/paginator/paginator.component';
import { NgPagination } from './components/paginator/ng-pagination/ng-pagination.component';
import { FormsModule } from '@angular/forms';
import { SortIconComponent } from './components/sort-icon/sort-icon.component';
@NgModule({
  exports: [PaginatorComponent, NgPagination, SortIconComponent],
  imports: [CommonModule, FormsModule, PaginatorComponent, NgPagination, SortIconComponent, ],
})
export class CRUDTableModule { }
