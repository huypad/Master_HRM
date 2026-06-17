import { Routes } from '@angular/router';

export const widgetsRoutes: Routes = [
  {
    path: 'advance-tables/widget1',
    loadComponent: () =>
      import('../widgets/advance-tables/advance-tables-widget1/advance-tables-widget1.component').then(
        (m) => m.AdvanceTablesWidget1Component
      ),
  },
  {
    path: 'advance-tables/widget2',
    loadComponent: () =>
      import('../widgets/advance-tables/advance-tables-widget2/advance-tables-widget2.component').then(
        (m) => m.AdvanceTablesWidget2Component
      ),
  },
  {
    path: 'lists/widget3',
    loadComponent: () =>
      import('../widgets/lists/lists-widget3/lists-widget3.component').then(
        (m) => m.ListsWidget3Component
      ),
  },
  {
    path: 'search',
    loadComponent: () =>
      import('../../layout/extras/dropdown-inner/search-dropdown-inner/search-dropdown-inner.component').then(
        (m) => m.SearchDropdownInnerComponent
      ),
  },
];