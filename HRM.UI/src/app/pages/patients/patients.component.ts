import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { PatientService } from '../../services/patient.service';
import { Patient } from '../../models/patient.model';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-patients',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="patients-page">
      <!-- Tiêu đề trang Quản lý Bệnh nhân -->
      <div class="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h3 class="fw-bold text-dark m-0">👥 Quản lý Bệnh nhân</h3>
          <p class="text-muted m-0 fs-7">Danh sách hồ sơ bệnh nhân trong CSDL HealthcareDB (Thêm mới, Cập nhật, Xóa)</p>
        </div>
        <div>
          <button (click)="openAddModal()" class="btn btn-primary fw-bold px-4 py-2 rounded-3 shadow-sm">
            <span>➕</span> Thêm Bệnh nhân mới
          </button>
        </div>
      </div>

      <!-- Card Bảng Danh sách Bệnh nhân -->
      <div class="card border-0 shadow-sm rounded-4">
        <div class="card-header bg-white border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between">
          <h5 class="fw-bold text-dark m-0">📋 Danh sách Bệnh nhân <span class="badge bg-light text-primary border rounded-pill ms-2 fs-7">{{ total }} bản ghi</span></h5>
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
                  <th scope="col" class="py-3">Email</th>
                  <th scope="col" class="py-3 text-end pe-3">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let item of patients">
                  <td class="ps-3 fw-bold text-primary">#{{ item.patientID }}</td>
                  <td class="fw-semibold text-dark">{{ item.name }}</td>
                  <td><code class="bg-light px-2 py-1 rounded text-dark fs-7">{{ item.cccd }}</code></td>
                  <td>{{ item.phone }}</td>
                  <td><code class="bg-light px-2 py-1 rounded text-dark fs-7">{{ item.bankAccount }}</code></td>
                  <td><span class="badge bg-secondary-subtle text-secondary rounded-pill px-3 py-1">{{ item.age ?? 'N/A' }}</span></td>
                  <td>
                    <span class="badge" [ngClass]="item.gender === 'Male' || item.gender === 'Nam' ? 'bg-primary-subtle text-primary' : 'bg-danger-subtle text-danger'">
                      {{ item.gender ?? 'N/A' }}
                    </span>
                  </td>
                  <td><span class="badge bg-warning-subtle text-dark fw-bold">{{ item.blood_Type ?? 'N/A' }}</span></td>
                  <td class="text-muted">{{ item.email ?? 'N/A' }}</td>
                  <td class="text-end pe-3">
                    <button (click)="openEditModal(item)" class="btn btn-sm btn-outline-primary me-2 rounded-2">
                      ✏️ Sửa
                    </button>
                    <button (click)="onDelete(item)" class="btn btn-sm btn-outline-danger rounded-2">
                      🗑️ Xóa
                    </button>
                  </td>
                </tr>

                <!-- Trạng thái đang tải dữ liệu -->
                <tr *ngIf="isLoading">
                  <td colspan="10" class="text-center py-5 text-muted">
                    <div class="spinner-border text-primary mb-2" role="status"></div>
                    <div class="fw-semibold">Đang tải danh sách bệnh nhân...</div>
                  </td>
                </tr>

                <!-- Trạng thái không có bản ghi -->
                <tr *ngIf="!isLoading && patients.length === 0">
                  <td colspan="10" class="text-center py-5 text-muted">
                    <div class="fs-1 mb-2">📭</div>
                    <div>Chưa có dữ liệu bệnh nhân nào.</div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Bộ điều khiển phân trang đồng bộ với màn hình /search -->
          <div *ngIf="total > 0" class="d-flex flex-wrap align-items-center justify-content-between pt-3 border-top mt-3">
            <!-- Thống kê vị trí bản ghi và chọn số dòng trên trang -->
            <div class="d-flex align-items-center mb-2 mb-md-0">
              <small class="text-muted me-3">
                Hiển thị từ <strong>{{ startRecord }}</strong> đến <strong>{{ endRecord }}</strong> trong tổng số <strong>{{ total }}</strong> bản ghi
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

            <!-- Các nút chuyển trang thông minh -->
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

      <!-- Modal Thêm mới / Cập nhật Bệnh nhân -->
      <div *ngIf="showModal" class="modal-backdrop-custom d-flex align-items-center justify-content-center">
        <div class="card border-0 shadow-lg rounded-4 modal-dialog-custom w-100" style="max-width: 650px;">
          <div class="card-header bg-white border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between">
            <h5 class="fw-bold text-dark m-0">{{ isEditMode ? '✏️ Sửa Thông tin Bệnh nhân' : '➕ Thêm Bệnh nhân Mới' }}</h5>
            <button (click)="closeModal()" type="button" class="btn-close" aria-label="Close"></button>
          </div>
          <div class="card-body p-4">
            <form (ngSubmit)="onSubmitForm()" class="row g-3">
              <!-- Họ và Tên -->
              <div class="col-md-6">
                <label class="form-label fw-semibold fs-7">Họ và Tên (*)</label>
                <input type="text" class="form-control" [(ngModel)]="formData.name" name="name" required placeholder="Nhập họ tên đầy đủ" />
              </div>

              <!-- Số CCCD -->
              <div class="col-md-6">
                <label class="form-label fw-semibold fs-7">Số CCCD / CMND (*)</label>
                <input type="text" class="form-control" [(ngModel)]="formData.cccd" name="cccd" required placeholder="Nhập số CCCD" />
              </div>

              <!-- Số Điện thoại -->
              <div class="col-md-6">
                <label class="form-label fw-semibold fs-7">Số Điện thoại (*)</label>
                <input type="text" class="form-control" [(ngModel)]="formData.phone" name="phone" required placeholder="Nhập số điện thoại" />
              </div>

              <!-- Số Tài khoản Bank -->
              <div class="col-md-6">
                <label class="form-label fw-semibold fs-7">Số Tài khoản Bank (*)</label>
                <input type="text" class="form-control" [(ngModel)]="formData.bankAccount" name="bankAccount" required placeholder="Nhập số tài khoản" />
              </div>

              <!-- Tuổi -->
              <div class="col-md-4">
                <label class="form-label fw-semibold fs-7">Tuổi (*)</label>
                <input type="number" class="form-control" [(ngModel)]="formData.age" name="age" required min="1" max="150" />
              </div>

              <!-- Giới tính -->
              <div class="col-md-4">
                <label class="form-label fw-semibold fs-7">Giới tính (*)</label>
                <select class="form-select" [(ngModel)]="formData.gender" name="gender">
                  <option value="Nam">Nam (Male)</option>
                  <option value="Nữ">Nữ (Female)</option>
                  <option value="Male">Male</option>
                  <option value="Female">Female</option>
                </select>
              </div>

              <!-- Nhóm máu -->
              <div class="col-md-4">
                <label class="form-label fw-semibold fs-7">Nhóm máu (*)</label>
                <select class="form-select" [(ngModel)]="formData.blood_Type" name="blood_Type">
                  <option value="O+">O+</option>
                  <option value="O-">O-</option>
                  <option value="A+">A+</option>
                  <option value="A-">A-</option>
                  <option value="B+">B+</option>
                  <option value="B-">B-</option>
                  <option value="AB+">AB+</option>
                  <option value="AB-">AB-</option>
                </select>
              </div>

              <!-- Email -->
              <div class="col-md-12">
                <label class="form-label fw-semibold fs-7">Địa chỉ Email (*)</label>
                <input type="email" class="form-control" [(ngModel)]="formData.email" name="email" required placeholder="example@gmail.com" />
              </div>

              <!-- Các nút bấm Footer -->
              <div class="col-md-12 d-flex justify-content-end gap-2 pt-3 border-top mt-3">
                <button type="button" (click)="closeModal()" class="btn btn-light fw-semibold px-4">Hủy bỏ</button>
                <button type="submit" class="btn btn-primary fw-bold px-4">
                  {{ isEditMode ? 'Lưu cập nhật' : 'Thêm bệnh nhân' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop-custom {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background-color: rgba(0, 0, 0, 0.5);
      z-index: 1050;
    }
  `]
})
export class PatientsComponent implements OnInit {
  patients: Patient[] = [];
  page: number = 1;
  pageSize: number = 10;
  total: number = 0;
  isLoading: boolean = false;

  showModal: boolean = false;
  isEditMode: boolean = false;
  editingId?: number;

  formData: Patient = this.getEmptyForm();

  constructor(
    private patientService: PatientService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  // Tính tổng số trang dựa trên tổng số bản ghi và pageSize
  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize) || 1;
  }

  // Chỉ số bản ghi bắt đầu hiển thị
  get startRecord(): number {
    return this.total === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  // Chỉ số bản ghi kết thúc hiển thị
  get endRecord(): number {
    return Math.min(this.page * this.pageSize, this.total);
  }

  // Tải danh sách bệnh nhân phân trang từ Backend API
  loadPatients(): void {
    this.isLoading = true;
    this.patientService.getPaged(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.patients = res.items ?? [];
        this.total = res.total ?? 0;
      },
      error: (err) => {
        this.isLoading = false;
        this.notificationService.showError(err?.error?.message || 'Lỗi khi tải danh sách bệnh nhân.');
      }
    });
  }

  // Chuyển tới trang cụ thể
  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages) {
      this.page = p;
      this.loadPatients();
    }
  }

  // Thay đổi số lượng dòng hiển thị trên mỗi trang
  onPageSizeChange(): void {
    this.page = 1;
    this.loadPatients();
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

  openAddModal(): void {
    this.isEditMode = false;
    this.editingId = undefined;
    this.formData = this.getEmptyForm();
    this.showModal = true;
  }

  openEditModal(item: Patient): void {
    this.isEditMode = true;
    this.editingId = item.patientID;
    this.formData = { ...item };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  // Gửi Form Thêm mới / Cập nhật
  onSubmitForm(): void {
    if (!this.formData.name || !this.formData.cccd) {
      this.notificationService.showError('Vui lòng điền đầy đủ Họ tên và Số CCCD.');
      return;
    }

    if (this.isEditMode && this.editingId) {
      this.patientService.update(this.editingId, this.formData).subscribe({
        next: (res) => {
          this.notificationService.showSuccess(res.message || 'Cập nhật thông tin thành công!');
          this.closeModal();
          this.loadPatients();
        }
      });
    } else {
      this.patientService.create(this.formData).subscribe({
        next: (created) => {
          this.notificationService.showSuccess(`Thêm mới thành công bệnh nhân #${created.patientID}!`);
          this.closeModal();
          this.loadPatients();
        }
      });
    }
  }

  // Xóa bệnh nhân
  onDelete(item: Patient): void {
    if (!item.patientID) return;

    if (confirm(`Bạn có chắc chắn muốn xóa bệnh nhân "${item.name}" (ID #${item.patientID}) không?`)) {
      this.patientService.delete(item.patientID).subscribe({
        next: (res) => {
          this.notificationService.showSuccess(res.message || 'Xóa bệnh nhân thành công!');
          this.loadPatients();
        }
      });
    }
  }

  private getEmptyForm(): Patient {
    return {
      name: '',
      cccd: '',
      phone: '',
      bankAccount: '',
      age: 30,
      gender: 'Nam',
      blood_Type: 'O+',
      email: ''
    };
  }
}
