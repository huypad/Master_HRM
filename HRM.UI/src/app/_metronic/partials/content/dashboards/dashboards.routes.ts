// dashboard.routes.ts
import { Routes } from '@angular/router';
import { Dashboard1Component } from './dashboard1/dashboard1.component';
import { Dashboard2Component } from './dashboard2/dashboard2.component';
import { Dashboard3Component } from './dashboard3/dashboard3.component';
import { DashboardWrapperComponent } from './dashboard-wrapper/dashboard-wrapper.component';

export const dashboardRoutes: Routes = [
  {
    path: 'dashboard1',
    component: Dashboard1Component,
  },
  {
    path: 'dashboard2',
    component: Dashboard2Component,
  },
  {
    path: 'dashboard3',
    component: Dashboard3Component,
  },
  {
    path: 'wrapper',
    component: DashboardWrapperComponent,
  },
];