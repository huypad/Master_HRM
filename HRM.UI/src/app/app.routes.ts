import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { generalRoutes } from './_metronic/partials/content/general/general.routes';
import { dropdownMenusRoutes } from './_metronic/partials/content/dropdown-menus/dropdown-menus.routes';
import { dashboardRoutes } from './_metronic/partials/content/dashboards/dashboards.routes';
import { coreRoutes } from './_metronic/core/core.routes';
import { extrasRoutes } from './_metronic/partials/layout/extras/extra.routes';
import { splashScreenRoutes } from './_metronic/partials/layout/splash-screen/splash-screen.routes';
import { crudTableRoutes } from './_metronic/shared/crud-table/crud-table.routes';
import { LayoutComponent } from './components/layouts/layout.component';
import { subheaderRoutes } from './_metronic/partials/layout/subheader/subheader.routes';
import { HomeComponent } from './components/layouts/components/home/home.component';
import { Error404Component } from './errors/error-404/error-404.component';
import { Error403Component } from './errors/error-403/error-403.component';
import { Error500Component } from './errors/error-500/error-500.component';
import { Error401Component } from './errors/error-401/error-401.component';
import { NhanVienListComponent } from './components/HRM/Management/NhanVienManagement/nhan-vien-list/nhan-vien-list.component';

export const routes: Routes = [
  { path: '', component: NhanVienListComponent },
  { path: 'error/401', component: Error401Component },
  { path: 'error/403', component: Error403Component },
  { path: 'error/404', component: Error404Component },
  { path: 'error/500', component: Error500Component },

  { path: '**', redirectTo: '/error/404', pathMatch: 'full' },
];
