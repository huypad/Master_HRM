// dropdown-menus.routes.ts
import { Routes } from '@angular/router';
import { DropdownMenu1Component } from './dropdown-menu1/dropdown-menu1.component';
import { DropdownMenu2Component } from './dropdown-menu2/dropdown-menu2.component';
import { DropdownMenu3Component } from './dropdown-menu3/dropdown-menu3.component';
import { DropdownMenu4Component } from './dropdown-menu4/dropdown-menu4.component';

export const dropdownMenusRoutes: Routes = [
  {
    path: 'dropdown-menu1',
    component: DropdownMenu1Component,
  },
  {
    path: 'dropdown-menu2',
    component: DropdownMenu2Component,
  },
  {
    path: 'dropdown-menu3',
    component: DropdownMenu3Component,
  },
  {
    path: 'dropdown-menu4',
    component: DropdownMenu4Component,
  },
];