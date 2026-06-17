// core.routes.ts
import { Routes } from '@angular/router';

export const coreRoutes: Routes = [
  // Pipe không gắn trực tiếp với route, nên có thể để trống hoặc thêm component placeholder nếu cần
  {
    path: '',
    redirectTo: '/login', // Hoặc một component placeholder nếu cần
    pathMatch: 'full',
  },
];