import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
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
          <p class="text-muted m-0 fs-7">Tìm kiếm dữ liệu bệnh nhân và đo đạc thời gian thực thi theo thuật toán mã hóa (V1 Baseline / V2 HMAC-BI-GRAM)</p>
        </div>
      </div>

      <!-- Card nhập liệu tìm kiếm -->
      <div class="card border-0 shadow-sm rounded-4 mb-4">
        <div class="card-body p-4">
          <form (ngSubmit)="onSearch()" class="row g-3 align-items-end">
            <!-- Input Từ khóa -->
            <div class="col-md-5">
              <label class="form-label fw-semibold text-secondary fs-7">Từ khóa tìm kiếm (Keyword)</label>
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
              <label class="form-label fw-semibold text-secondary fs-7">Trường tra cứu (Field)</label>
              <select class="form-select" [(ngModel)]="field" name="field">
                <option value="Name">Họ tên (Name)</option>
                <option value="ID">Mã Bệnh nhân (ID)</option>
                <option value="CCCD">Số CCCD / CMND</option>
                <option value="Phone">Số Điện thoại (Phone)</option>
                <option value="Bank">Số Tài khoản (Bank)</option>
              </select>
            </div>

            <!-- Dropdown Pipeline thuật toán -->
            <div class="col-md-2">
              <label class="form-label fw-semibold text-secondary fs-7">Pipeline thuật toán</label>
              <select class="form-select" [(ngModel)]="pipeline" name="pipeline">
                <option value="V2">V2 (HMAC + BI-GRAM)</option>
                <option value="V1">V1 (Baseline SHA256)</option>
              </select>
            </div>

            <!-- Nút Tìm kiếm -->
            <div class="col-md-2">
              <button type="submit" class="btn btn-primary w-100 fw-bold py-2 shadow-sm rounded-3">
                <span>🔎</span> Tìm kiếm
              </button>
            </div>
          </form>
        </div>
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
          <div class="card border-0 shadow-sm rounded-3 bg-secondary text-white h-100">
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
                <tr *ngFor="let item of items">
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
                <tr *ngIf="items.length === 0">
                  <td colspan="9" class="text-center py-5 text-muted">
                    <div class="fs-1 mb-2">📭</div>
                    <div>Không tìm thấy bản ghi bệnh nhân nào phù hợp.</div>
                  </td>
                </tr>
              </tbody>
            </table>
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

  items: Patient[] = [];
  totalResults: number = 0;
  searchDebug: SearchDebugInfo | null = null;

  constructor(
    private patientService: PatientService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    // Tải danh sách 10 bệnh nhân ban đầu từ CSDL
    this.loadInitialPatients();
  }

  // Tải danh sách bệnh nhân ban đầu từ API
  private loadInitialPatients(): void {
    this.patientService.getPaged(1, 10).subscribe({
      next: (res) => {
        this.items = res.items ?? [];
        this.totalResults = res.total ?? 0;
      }
    });
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
      this.notificationService.showError('Vui lòng nhập từ khóa tìm kiếm (keyword).');
      return;
    }

    this.patientService.searchBenchmark(trimmed, this.field, this.pipeline).subscribe({
      next: (res) => {
        this.items = res.items ?? [];
        this.totalResults = res.total ?? 0;
        this.searchDebug = res.searchDebug ?? null;
      }
    });
  }
}
