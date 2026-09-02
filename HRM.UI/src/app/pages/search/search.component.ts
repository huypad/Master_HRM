import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { timeout, catchError } from 'rxjs/operators';
import { of, throwError } from 'rxjs';
import { PatientService } from '../../services/patient.service';
import { Patient, SearchDebugInfo } from '../../models/patient.model';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="search-page">
      <!-- Tiêu đề trang tra cứu -->
      <div class="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h3 class="fw-bold text-dark m-0">🔍 Tra cứu &amp; Benchmark Lẻ</h3>
          <p class="text-muted m-0 fs-7">Tìm kiếm dữ liệu bệnh nhân và đo đạc thời gian thực thi theo thuật toán mã hóa (V1 Baseline / V2 HMAC-TRI-GRAM)</p>
        </div>
      </div>

      <!-- Card nhập liệu tìm kiếm -->
      <div class="card border-0 shadow-sm rounded-4 mb-4">
        <div class="card-body p-4">
          <form (ngSubmit)="onSearch()" class="row g-3 align-items-end">
            <!-- Input Từ khóa -->
            <div class="col-md-5">
              <label class="form-label fw-semibold text-secondary fs-7">Từ khóa tìm kiếm </label>
              <div class="input-group">
                <span class="input-group-text bg-light border-end-0">🔍</span>
                <input
                  type="text"
                  class="form-control border-start-0 ps-0"
                  [(ngModel)]="keyword"
                  name="keyword"
                  placeholder="Nhập từ khóa (Ví dụ: Mạc Thanh Trâm, 1200008248...)"
                />
              </div>
            </div>

            <!-- Dropdown Trường tra cứu -->
            <div class="col-md-3">
              <label class="form-label fw-semibold text-secondary fs-7">Trường tra cứu  </label>
              <select class="form-select" [(ngModel)]="field" name="field">
                <option value="Name">Họ tên</option>
                <option value="ID">Mã Bệnh nhân (ID)</option>
                <option value="CCCD">Số CCCD / CMND</option>
                <option value="Phone">Số Điện thoại</option>
                <option value="Bank">Số Tài khoản</option>
              </select>
            </div>

            <!-- Dropdown Pipeline thuật toán -->
            <div class="col-md-2">
              <label class="form-label fw-semibold text-secondary fs-7">Pipeline thuật toán</label>
              <select class="form-select" [(ngModel)]="pipeline" name="pipeline">
                <option value="V2">V2 (HMAC + TRI-GRAM)</option>
                <option value="V1">V1 (Baseline SHA256)</option>
              </select>
            </div>

            <!-- Nút Tìm kiếm -->
            <div class="col-md-2">
              <button type="submit" class="btn btn-primary w-100 fw-bold py-2 shadow-sm rounded-3" [disabled]="isLoading">
                <span *ngIf="isLoading" class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                <span *ngIf="!isLoading">🔎</span> {{ isLoading ? 'Đang tra cứu...' : 'Tìm kiếm' }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Thông báo lỗi Timeout -->
      <div *ngIf="v1TimeoutError" class="alert alert-danger border border-3 border-danger shadow-sm rounded-4 p-4 mb-4 text-center" style="background-color: #fff2f2;">
        <div class="display-6 mb-2 text-danger">⚠️ </div>
        <h4 class="fw-bold text-danger mb-2">CẢNH BÁO QUÁ TẢI HỆ THỐNG (TIMEOUT)</h4>
        <p class="fs-5 fw-bold text-danger mb-1">
          {{ v1TimeoutError }}
        </p>
        <small class="text-danger fw-semibold d-block">
          (Quá thời gian chờ 15 giây: Hệ thống V1 Baseline SHA256 phải quét và giải mã toàn bộ bản ghi)
        </small>
      </div>

      <!-- Khối thống kê chỉ số SearchDebugInfo từ Backend API -->
      <div *ngIf="searchDebug" class="row g-3 mb-4">
        <!-- Tổng thời gian -->
        <div class="col-md-2 col-sm-4">
          <div class="card border-0 shadow-sm rounded-3 bg-primary text-white h-100">
            <div class="card-body p-3 text-center">
              <small class="text-white-50 text-uppercase fw-bold fs-8">Tổng thời gian</small>
              <h3 class="fw-bold my-1">{{ searchDebug.totalMs ?? 0 }} <span class="fs-6">ms</span></h3>
              <small class="fs-8">Total Execution</small>
            </div>
          </div>
        </div>

        <!-- SQL Step 1 -->
        <div class="col-md-2 col-sm-4">
          <div class="card border-0 shadow-sm rounded-3 bg-info text-white h-100">
            <div class="card-body p-3 text-center">
              <small class="text-white-50 text-uppercase fw-bold fs-8">SQL Index Step 1</small>
              <h3 class="fw-bold my-1">{{ searchDebug.step1Ms ?? 0 }} <span class="fs-6">ms</span></h3>
              <small class="fs-8">SQL Querying</small>
            </div>
          </div>
        </div>

        <!-- Giải mã Step 2 -->
        <div class="col-md-2 col-sm-4">
          <div class="card border-0 shadow-sm rounded-3 bg-success text-white h-100">
            <div class="card-body p-3 text-center">
              <small class="text-white-50 text-uppercase fw-bold fs-8">Giải mã Step 2</small>
              <h3 class="fw-bold my-1">{{ searchDebug.step2Ms ?? 0 }} <span class="fs-6">ms</span></h3>
              <small class="fs-8">Decrypt &amp; Filter</small>
            </div>
          </div>
        </div>

        <!-- Số ứng viên Candidate -->
        <div class="col-md-2 col-sm-4">
          <div class="card border-0 shadow-sm rounded-3 bg-dark text-white h-100">
            <div class="card-body p-3 text-center">
              <small class="text-white-50 text-uppercase fw-bold fs-8">Ứng viên SQL</small>
              <h3 class="fw-bold my-1">{{ searchDebug.candidateCount ?? 0 }}</h3>
              <small class="fs-8">Candidates</small>
            </div>
          </div>
        </div>

        <!-- Số đụng độ Collisions -->
        <div class="col-md-2 col-sm-4">
          <div class="card border-0 shadow-sm rounded-3 bg-warning text-dark h-100">
            <div class="card-body p-3 text-center">
              <small class="text-dark-50 text-uppercase fw-bold fs-8">Số Đụng độ (Nhiễu)</small>
              <h3 class="fw-bold my-1 text-dark">{{ getCollisionCount() }}</h3>
              <small class="fs-8 text-dark-50">Collisions</small>
            </div>
          </div>
        </div>

        <!-- Kết quả khớp Matches -->
        <div class="col-md-2 col-sm-4">
          <div class="card border-0 shadow-sm rounded-3 text-white h-100" style="background-color: #FF70A6;">
            <div class="card-body p-3 text-center">
              <small class="text-white-50 text-uppercase fw-bold fs-8">Kết quả khớp</small>
              <h3 class="fw-bold my-1">{{ searchDebug.resultCount ?? items.length }}</h3>
              <small class="fs-8">Matches</small>
            </div>
          </div>
        </div>
      </div>

      <!-- Bảng kết quả dữ liệu Bệnh nhân -->
      <div class="card border-0 shadow-sm rounded-4">
        <div class="card-header bg-white border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between">
          <h5 class="fw-bold text-dark m-0">📋 Danh sách Bệnh nhân <span class="badge bg-light text-primary border rounded-pill ms-2 fs-7">{{ totalResults }} bản ghi</span></h5>
        </div>
        <div class="card-body p-4">
          <div class="table-responsive">
            <table class="table table-hover align-middle">
              <thead class="table-light text-secondary fs-7 text-uppercase">
                <tr>
                  <th scope="col" class="py-3 ps-3">ID</th>
                  <th scope="col" class="py-3">Họ và Tên</th>
                  <th scope="col" class="py-3">Số CCCD</th>
                  <th scope="col" class="py-3">Số Điện thoại</th>
                  <th scope="col" class="py-3">Số Tài khoản</th>
                  <th scope="col" class="py-3">Tuổi</th>
                  <th scope="col" class="py-3">Giới tính</th>
                  <th scope="col" class="py-3">Nhóm máu</th>
                  <th scope="col" class="py-3 pe-3">Email</th>
                </tr>
              </thead>
              <tbody>
                <!-- Render danh sách theo trang pagedItems để tránh giật lag khi có 900 bản ghi -->
                <tr *ngFor="let item of pagedItems">
                  <td class="ps-3 fw-bold text-primary">#{{ item.patientID }}</td>
                  <td class="fw-semibold text-dark">{{ item.name }}</td>
                  <td><code class="bg-light px-2 py-1 rounded text-dark fs-7">{{ item.cccd }}</code></td>
                  <td>{{ item.phone }}</td>
                  <td><code class="bg-light px-2 py-1 rounded text-dark fs-7">{{ item.bankAccount }}</code></td>
                  <td><span class="badge bg-secondary-subtle text-secondary rounded-pill px-3 py-1">{{ item.age ?? 'N/A' }}</span></td>
                  <td>
                    <span class="badge" [ngClass]="item.gender === 'Male' ? 'bg-primary-subtle text-primary' : 'bg-danger-subtle text-danger'">
                      {{ item.gender ?? 'N/A' }}
                    </span>
                  </td>
                  <td><span class="badge bg-warning-subtle text-dark fw-bold">{{ item.blood_Type ?? 'N/A' }}</span></td>
                  <td class="pe-3 text-muted">{{ item.email ?? 'N/A' }}</td>
                </tr>

                <!-- Trạng thái đang tải dữ liệu -->
                <tr *ngIf="isLoading">
                  <td colspan="9" class="text-center py-5 text-muted">
                    <div class="spinner-border text-primary mb-2" role="status"></div>
                    <div class="fw-semibold">Đang truy vấn và giải mã dữ liệu...</div>
                  </td>
                </tr>

                <!-- Trạng thái không có bản ghi -->
                <tr *ngIf="!isLoading && items.length === 0">
                  <td colspan="9" class="text-center py-5 text-muted">
                    <div class="fs-1 mb-2">📭</div>
                    <div>Không tìm thấy bản ghi bệnh nhân nào phù hợp.</div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Bộ điều khiển phân trang -->
          <div *ngIf="items.length > 0" class="d-flex flex-wrap align-items-center justify-content-between pt-3 border-top mt-3">
            <!-- Thống kê vị trí bản ghi và số dòng trên trang -->
            <div class="d-flex align-items-center mb-2 mb-md-0">
              <small class="text-muted me-3">
                Hiển thị từ <strong>{{ startRecord }}</strong> đến <strong>{{ endRecord }}</strong> trong tổng số <strong>{{ items.length }}</strong> bản ghi
              </small>
              <div class="d-flex align-items-center">
                <small class="text-muted me-2">Số dòng/trang:</small>
                <select class="form-select form-select-sm" style="width: auto;" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()">
                  <option [ngValue]="10">10</option>
                  <option [ngValue]="25">25</option>
                  <option [ngValue]="50">50</option>
                  <option [ngValue]="100">100</option>
                </select>
              </div>
            </div>

            <!-- Các nút chuyển trang -->
            <div class="btn-group">
              <button (click)="goToPage(1)" [disabled]="page <= 1" class="btn btn-sm btn-outline-secondary" title="Trang đầu">
                « Đầu
              </button>
              <button (click)="goToPage(page - 1)" [disabled]="page <= 1" class="btn btn-sm btn-outline-secondary">
                ‹ Trước
              </button>

              <ng-container *ngFor="let p of getVisiblePages()">
                <button
                  *ngIf="p !== -1"
                  (click)="goToPage(p)"
                  [class.btn-primary]="p === page"
                  [class.text-white]="p === page"
                  [class.btn-outline-secondary]="p !== page"
                  class="btn btn-sm"
                >
                  {{ p }}
                </button>
                <button *ngIf="p === -1" class="btn btn-sm btn-outline-secondary disabled" disabled>
                  ...
                </button>
              </ng-container>

              <button (click)="goToPage(page + 1)" [disabled]="page >= totalPages" class="btn btn-sm btn-outline-secondary">
                Sau ›
              </button>
              <button (click)="goToPage(totalPages)" [disabled]="page >= totalPages" class="btn btn-sm btn-outline-secondary" title="Trang cuối">
                Cuối »
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class SearchComponent implements OnInit {
  keyword: string = '';
  field: string = 'Name';
  pipeline: string = 'V2';

  // Danh sách toàn bộ kết quả trả về từ API
  items: Patient[] = [];
  totalResults: number = 0;
  searchDebug: SearchDebugInfo | null = null;

  // Trạng thái tải dữ liệu và thông báo lỗi V1
  isLoading: boolean = false;
  v1TimeoutError: string | null = null;

  // Biến quản lý phân trang
  page: number = 1;
  pageSize: number = 10;

  constructor(
    private patientService: PatientService,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    // Tải danh sách các bệnh nhân ban đầu từ CSDL
    this.loadInitialPatients();
  }

  // Tải danh sách bệnh nhân ban đầu từ API
  private loadInitialPatients(): void {
    this.patientService.getPaged(1, 10).subscribe({
      next: (res) => {
        this.items = res.items ?? [];
        this.totalResults = res.total ?? 0;
        this.page = 1;
      }
    });
  }

  // Tính tổng số trang dựa trên độ dài danh sách items và pageSize
  get totalPages(): number {
    return Math.ceil(this.items.length / this.pageSize) || 1;
  }

  // Lấy dữ liệu cho trang hiện tại 
  get pagedItems(): Patient[] {
    const start = (this.page - 1) * this.pageSize;
    return this.items.slice(start, start + this.pageSize);
  }

  // Chỉ số bản ghi bắt đầu hiển thị
  get startRecord(): number {
    return this.items.length === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  // Chỉ số bản ghi kết thúc hiển thị
  get endRecord(): number {
    return Math.min(this.page * this.pageSize, this.items.length);
  }

  // Chuyển tới trang cụ thể
  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages) {
      this.page = p;
    }
  }

  // Thay đổi số lượng dòng hiển thị trên mỗi trang
  onPageSizeChange(): void {
    this.page = 1;
  }

  // Tạo danh sách số trang hiển thị thông minh 
  getVisiblePages(): number[] {
    const total = this.totalPages;
    const current = this.page;
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) {
        pages.push(i);
      }
    } else {
      pages.push(1);
      if (current > 3) {
        pages.push(-1); // Đại diện cho dấu '...'
      }

      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);
      for (let i = start; i <= end; i++) {
        pages.push(i);
      }

      if (current < total - 2) {
        pages.push(-1); // Đại diện cho dấu '...'
      }
      pages.push(total);
    }

    return pages;
  }

  // Tính số lượng đụng độ (Collision Count = candidateCount - resultCount)
  getCollisionCount(): number {
    if (!this.searchDebug) return 0;
    if (this.searchDebug.collisionCount !== undefined && this.searchDebug.collisionCount !== null) {
      return this.searchDebug.collisionCount;
    }
    const candidate = this.searchDebug.candidateCount ?? 0;
    const result = this.searchDebug.resultCount ?? this.items.length;
    return Math.max(0, candidate - result);
  }

  // Thực thi tìm kiếm benchmark đơn lẻ
  onSearch(): void {
    const trimmed = this.keyword.trim();
    if (!trimmed) {
      this.notificationService.showError('Vui lòng nhập từ khóa tìm kiếm.');
      return;
    }

    // Đặt lại trạng thái trước khi tìm kiếm mới
    this.isLoading = true;
    this.v1TimeoutError = null;
    this.page = 1;

    this.patientService
      .searchBenchmark(trimmed, this.field, this.pipeline)
      .pipe(
        // Giới hạn thời gian phản hồi là 15 giây 
        timeout({
          each: 15000,
          with: () => throwError(() => new Error('TIMEOUT_15S'))
        }),
        catchError((err) => {
          this.isLoading = false;
          // Bắt lỗi khi V1 bị quá tải hoặc phản hồi quá 15 giây
          if (this.pipeline === 'V1' || err?.message === 'TIMEOUT_15S') {
            this.v1TimeoutError = 'Hệ thống V1 bị quá tải bộ nhớ do phải quét toàn bộ dữ liệu mã hóa!';
            this.notificationService.showError(this.v1TimeoutError);
            this.items = [];
            this.totalResults = 0;
            this.searchDebug = null;
          } else {
            this.notificationService.showError(err?.error?.message || 'Có lỗi xảy ra khi tra cứu dữ liệu.');
          }
          return of(null);
        })
      )
      .subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res) {
            this.items = res.items ?? [];
            this.totalResults = res.total ?? 0;
            this.searchDebug = res.searchDebug ?? null;
            this.page = 1;
          }
        }
      });
  }
}
