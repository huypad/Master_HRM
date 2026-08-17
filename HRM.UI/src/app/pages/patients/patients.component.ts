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
      <!-- Title Header -->
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

      <!-- Patients Table Card -->
      <div class="card border-0 shadow-sm rounded-4">
        <div class="card-header bg-white border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between">
          <h5 class="fw-bold text-dark m-0">📋 Danh sách Bệnh nhân <span class="badge bg-light text-primary border rounded-pill ms-2 fs-7">Tổng: {{ total }} bản ghi</span></h5>
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
                  <td><span class="badge bg-secondary-subtle text-secondary rounded-pill px-3 py-1">{{ item.age }}</span></td>
                  <td>
                    <span class="badge" [ngClass]="item.gender === 'Male' ? 'bg-primary-subtle text-primary' : 'bg-danger-subtle text-danger'">
                      {{ item.gender }}
                    </span>
                  </td>
                  <td><span class="badge bg-warning-subtle text-dark fw-bold">{{ item.blood_Type }}</span></td>
                  <td class="text-muted">{{ item.email }}</td>
                  <td class="text-end pe-3">
                    <button (click)="openEditModal(item)" class="btn btn-sm btn-outline-primary me-2 rounded-2">
                      ✏️ Sửa
                    </button>
                    <button (click)="onDelete(item)" class="btn btn-sm btn-outline-danger rounded-2">
                      🗑️ Xóa
                    </button>
                  </td>
                </tr>
                <tr *ngIf="patients.length === 0">
                  <td colspan="10" class="text-center py-5 text-muted">
                    <div class="fs-1 mb-2">📭</div>
                    <div>Chưa có dữ liệu bệnh nhân nào.</div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination Controls -->
          <div class="d-flex align-items-center justify-content-between pt-3 border-top mt-3">
            <small class="text-muted">
              Hiển thị trang <strong>{{ page }}</strong> / <strong>{{ totalPages }}</strong> (Tổng {{ total }} bản ghi)
            </small>
            <div class="btn-group">
              <button (click)="changePage(page - 1)" [disabled]="page <= 1" class="btn btn-sm btn-outline-secondary">
                ◀ Trang trước
              </button>
              <button (click)="changePage(page + 1)" [disabled]="page >= totalPages" class="btn btn-sm btn-outline-secondary">
                Trang sau ▶
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Add / Edit Patient Modal Backdrop & Dialog -->
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
                  <option value="Male">Nam (Male)</option>
                  <option value="Female">Nữ (Female)</option>
                </select>
              </div>

              <!-- Nhóm máu -->
              <div class="col-md-4">
                <label class="form-label fw-semibold fs-7">Nhóm máu (blood_Type) (*)</label>
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

              <!-- Footer Buttons -->
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

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize) || 1;
  }

  // Tải danh sách bệnh nhân phân trang từ API
  loadPatients(): void {
    this.patientService.getPaged(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.patients = res.items ?? [];
        this.total = res.total ?? 0;
      }
    });
  }

  changePage(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.page = newPage;
      this.loadPatients();
    }
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
      gender: 'Male',
      blood_Type: 'O+',
      email: ''
    };
  }
}
