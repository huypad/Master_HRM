import { Routes } from '@angular/router';
import { NavLayoutComponent } from './components/layout/nav-layout.component';
import { SearchComponent } from './pages/search/search.component';
import { PatientsComponent } from './pages/patients/patients.component';
import { BenchmarkComponent } from './pages/benchmark/benchmark.component';

export const routes: Routes = [
  {
    path: '',
    component: NavLayoutComponent,
    children: [
      { path: '', redirectTo: 'search', pathMatch: 'full' },
      { path: 'search', component: SearchComponent },
      { path: 'patients', component: PatientsComponent },
      { path: 'benchmark', component: BenchmarkComponent }
    ]
  },
  { path: '**', redirectTo: 'search' }
];
