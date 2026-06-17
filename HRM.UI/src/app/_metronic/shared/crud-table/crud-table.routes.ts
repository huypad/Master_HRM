import { Route } from '@angular/router';
import { PaginatorComponent } from './components/paginator/paginator.component';
import { NgPagination } from './components/paginator/ng-pagination/ng-pagination.component';
import { SortIconComponent } from './components/sort-icon/sort-icon.component';

export const crudTableRoutes: Route[] = [
  {
    path: 'crud-table', // Đường dẫn cha cho các component CRUD table
    children: [
      {
        path: '',
        component: PaginatorComponent, // Trang mặc định khi truy cập /crud-table
      },
      {
        path: 'pagination',
        component: NgPagination, // Route cho NgPagination
      },
      {
        path: 'sort-icon',
        component: SortIconComponent, // Route cho SortIcon
      },
    ],
  },
];