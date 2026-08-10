import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LoadingSpinnerComponent } from '../HRM/_shared/loading/loading-spinner.component';

@Component({
  selector: 'app-nav-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, LoadingSpinnerComponent],
  template: `
    <!-- Top Navigation Bar Header -->
    <header class="navbar-header shadow-sm bg-white border-bottom sticky-top">
      <div class="container-fluid d-flex align-items-center justify-content-between py-2 px-4">
        
        <!-- Brand Title & Logo -->
        <div class="d-flex align-items-center">
          <div class="brand-badge me-3 bg-primary text-white rounded-3 d-flex align-items-center justify-content-center fw-bold shadow-sm" style="width: 40px; height: 40px; font-size: 1.2rem;">
            🏥
          </div>
          <div>
            <h5 class="m-0 fw-bold text-dark">Healthcare Benchmark</h5>
            <small class="text-muted" style="font-size: 0.78rem;">Tra Cứu &amp; Đánh Giá Hiệu Năng Mã Hóa Dữ Liệu</small>
          </div>
        </div>

        <!-- 3 Route Navigation Links -->
        <nav class="nav nav-pills">
          <a
            routerLink="/search"
            routerLinkActive="active"
            class="nav-link px-3 py-2 me-2 rounded-3 fw-semibold transition-all d-flex align-items-center"
          >
            <span class="me-2">🔍</span> Tra cứu &amp; Benchmark
          </a>
          
          <a
            routerLink="/patients"
            routerLinkActive="active"
            class="nav-link px-3 py-2 me-2 rounded-3 fw-semibold transition-all d-flex align-items-center"
          >
            <span class="me-2">👥</span> Quản lý Bệnh nhân
          </a>
          
          <a
            routerLink="/benchmark"
            routerLinkActive="active"
            class="nav-link px-3 py-2 rounded-3 fw-semibold transition-all d-flex align-items-center"
          >
            <span class="me-2">⚡</span> So sánh Thuật toán
          </a>
        </nav>
      </div>
    </header>

    <!-- Global Loading Spinner Overlay -->
    <app-loading-spinner></app-loading-spinner>

    <!-- Main Content Container -->
    <main class="main-content-container py-4 bg-light min-vh-100">
      <div class="container-fluid px-4">
        <router-outlet></router-outlet>
      </div>
    </main>
  `,
  styles: [`
    .navbar-header {
      z-index: 1030;
    }

    .nav-link {
      color: #495057;
      background-color: transparent;
      border: 1px solid transparent;
      font-size: 0.92rem;
      transition: all 0.2s ease-in-out;
    }

    .nav-link:hover {
      color: #0d6efd;
      background-color: #f8f9fa;
    }

    .nav-link.active {
      color: #ffffff !important;
      background-color: #0d6efd !important;
      box-shadow: 0 4px 10px rgba(13, 110, 253, 0.25);
    }

    .main-content-container {
      background-color: #f4f6f9 !important;
    }
  `]
})
export class NavLayoutComponent {}
