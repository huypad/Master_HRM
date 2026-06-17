import { Routes } from '@angular/router';

export const subheaderRoutes: Routes = [
  {
    path: 'subheader1',
    loadComponent: () =>
      import('./subheader1/subheader1.component').then(m => m.Subheader1Component),
  },
  {
    path: 'subheader2',
    loadComponent: () =>
      import('./subheader2/subheader2.component').then(m => m.Subheader2Component),
  },
  {
    path: 'subheader3',
    loadComponent: () =>
      import('./subheader3/subheader3.component').then(m => m.Subheader3Component),
  },
  {
    path: 'subheader4',
    loadComponent: () =>
      import('./subheader4/subheader4.component').then(m => m.Subheader4Component),
  },
  {
    path: 'subheader5',
    loadComponent: () =>
      import('./subheader5/subheader5.component').then(m => m.Subheader5Component),
  },
  {
    path: 'subheader6',
    loadComponent: () =>
      import('./subheader6/subheader6.component').then(m => m.Subheader6Component),
  },
  {
    path: 'subheader7',
    loadComponent: () =>
      import('./subheader7/subheader7.component').then(m => m.Subheader7Component),
  },
];
