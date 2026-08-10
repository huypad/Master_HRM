import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  NgApexchartsModule,
  ChartComponent,
  ApexAxisChartSeries,
  ApexChart,
  ApexXAxis,
  ApexDataLabels,
  ApexPlotOptions,
  ApexTooltip,
  ApexLegend,
  ApexStroke
} from 'ng-apexcharts';
import { PatientService } from '../../services/patient.service';
import { CompareBenchmarkResult } from '../../models/patient.model';
import { NotificationService } from '../../services/notification.service';

export type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  plotOptions: ApexPlotOptions;
  dataLabels: ApexDataLabels;
  tooltip: ApexTooltip;
  legend: ApexLegend;
  stroke: ApexStroke;
  colors: string[];
};

@Component({
  selector: 'app-benchmark',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NgApexchartsModule],
  template: `
    <div class="benchmark-page">
      <!-- Tiêu đề trang so sánh -->
      <div class="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h3 class="fw-bold text-dark m-0">⚡ So sánh Thuật toán V1 vs BI-GRAM V2</h3>
          <p class="text-muted m-0 fs-7">Chạy song song V1 (SHA256 Baseline) và V2 (HMAC + BI-GRAM Bucket) để đo đạc và so sánh tốc độ tăng tốc (Speedup Ratio)</p>
        </div>
      </div>

      <!-- Form nhập từ khóa và chọn trường tra cứu -->
      <div class="card border-0 shadow-sm rounded-4 mb-4">
        <div class="card-body p-4">
          <form (ngSubmit)="onCompare()" class="row g-3 align-items-end">
            <!-- Input Từ khóa -->
            <div class="col-md-6">
              <label class="form-label fw-semibold text-secondary fs-7">Từ khóa tra cứu (Keyword)</label>
              <div class="input-group">
                <span class="input-group-text bg-light border-end-0">🔍</span>
                <input
                  type="text"
                  class="form-control border-start-0 ps-0"
                  [(ngModel)]="keyword"
                  name="keyword"
                  placeholder="Nhập từ khóa so sánh (Ví dụ: Mạc Thanh Trâm, 1200008248...)"
                />
              </div>
            </div>

            <!-- Select Trường tra cứu -->
            <div class="col-md-4">
              <label class="form-label fw-semibold text-secondary fs-7">Trường tra cứu (Field)</label>
              <select class="form-select" [(ngModel)]="field" name="field">
                <option value="Name">Họ tên (Name)</option>
                <option value="CCCD">Số CCCD / CMND</option>
                <option value="Phone">Số Điện thoại (Phone)</option>
                <option value="Bank">Số Tài khoản (Bank)</option>
              </select>
            </div>

            <!-- Nút So sánh -->
            <div class="col-md-2">
              <button type="submit" class="btn btn-danger w-100 fw-bold py-2 shadow-sm rounded-3">
                <span>⚡</span> So sánh ngay
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Kết quả khi đã nhận dữ liệu từ Backend API -->
      <div *ngIf="compareResult">
        <!-- Banner kết quả phương án thắng & Tỷ lệ tăng tốc -->
        <div class="row g-3 mb-4">
          <!-- Card Winner -->
          <div class="col-md-4">
            <div
              class="card border-0 shadow-sm rounded-4 h-100 text-white p-3"
              [ngClass]="compareResult.comparison.winner === 'V2' ? 'bg-success' : 'bg-primary'"
            >
              <div class="card-body d-flex flex-column justify-content-between">
                <div>
                  <div class="d-flex align-items-center justify-content-between">
                    <small class="text-white-50 text-uppercase fw-bold fs-8">Phương án Chiến thắng</small>
                    <span class="badge bg-white text-dark rounded-pill px-3 py-1 fw-bold">WINNER</span>
                  </div>
                  <h1 class="display-3 fw-extrabold my-2">
                    {{ compareResult.comparison.winner }}
                  </h1>
                  <p class="m-0 text-white-50 fs-7">
                    Thuật toán {{ compareResult.comparison.winner === 'V2' ? 'V2 (HMAC + BI-GRAM Index)' : 'V1 (SHA256 Baseline)' }} đạt hiệu năng vượt trội hơn.
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- Card Total Speedup Ratio -->
          <div class="col-md-4">
            <div class="card border-0 shadow-sm rounded-4 h-100 bg-white p-3">
              <div class="card-body d-flex flex-column justify-content-between">
                <small class="text-muted text-uppercase fw-bold fs-8">Tỷ lệ Tăng tốc Tổng thể (Total Speedup)</small>
                <h1 class="display-4 fw-bold text-primary my-2">
                  {{ compareResult.comparison.totalSpeedupRatio }}x
                </h1>
                <small class="text-secondary fs-7">Tốc độ xử lý toàn bộ luồng V1 / V2</small>
              </div>
            </div>
          </div>

          <!-- Card Step 1 Speedup Ratio -->
          <div class="col-md-4">
            <div class="card border-0 shadow-sm rounded-4 h-100 bg-white p-3">
              <div class="card-body d-flex flex-column justify-content-between">
                <small class="text-muted text-uppercase fw-bold fs-8">Tỷ lệ Tăng tốc SQL Index (Step 1)</small>
                <h1 class="display-4 fw-bold text-info my-2">
                  {{ compareResult.comparison.step1SpeedupRatio }}x
                </h1>
                <small class="text-secondary fs-7">Tốc độ truy vấn CSDL SQL V1 / V2</small>
              </div>
            </div>
          </div>
        </div>

        <!-- Biểu đồ cột ApexCharts -->
        <div class="card border-0 shadow-sm rounded-4 mb-4">
          <div class="card-header bg-white border-0 pt-4 px-4 pb-0 d-flex align-items-center justify-content-between">
            <h5 class="fw-bold text-dark m-0">📊 Biểu đồ Cột So sánh Thời gian Thực thi (ms)</h5>
          </div>
          <div class="card-body p-4">
            <apx-chart
              *ngIf="chartOptions"
              [series]="chartOptions.series"
              [chart]="chartOptions.chart"
              [xaxis]="chartOptions.xaxis"
              [plotOptions]="chartOptions.plotOptions"
              [dataLabels]="chartOptions.dataLabels"
              [tooltip]="chartOptions.tooltip"
              [legend]="chartOptions.legend"
              [colors]="chartOptions.colors"
            ></apx-chart>
          </div>
        </div>

        <!-- Bảng chi tiết thông số so sánh -->
        <div class="card border-0 shadow-sm rounded-4">
          <div class="card-header bg-white border-0 pt-4 px-4 pb-0">
            <h5 class="fw-bold text-dark m-0">📋 Bảng Chi tiết Thông số Hiệu năng V1 vs BI-GRAM V2</h5>
          </div>
          <div class="card-body p-4">
            <div class="table-responsive">
              <table class="table table-bordered align-middle">
                <thead class="table-light text-center fs-7 text-uppercase">
                  <tr>
                    <th scope="col" class="py-3 text-start ps-3" style="width: 34%;">Chỉ số Đánh giá (Metrics)</th>
                    <th scope="col" class="py-3 text-primary" style="width: 33%;">V1 Baseline (SHA256)</th>
                    <th scope="col" class="py-3 text-success" style="width: 33%;">V2 Cải tiến (HMAC + BI-GRAM)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <td class="fw-semibold ps-3">Mô tả Loại Tìm kiếm (Search Type)</td>
                    <td class="text-center"><code>{{ compareResult.v1.searchDebug?.searchType ?? 'Fuzzy_V1_Baseline' }}</code></td>
                    <td class="text-center"><code>{{ compareResult.v2.searchDebug?.searchType ?? 'Fuzzy_V2_BI-GRAM' }}</code></td>
                  </tr>
                  <tr>
                    <td class="fw-semibold ps-3">Tổng Thời gian Thực thi (Total Ms)</td>
                    <td class="text-center fw-bold text-danger">{{ compareResult.v1.searchDebug?.totalMs ?? 0 }} ms</td>
                    <td class="text-center fw-bold text-success">{{ compareResult.v2.searchDebug?.totalMs ?? 0 }} ms</td>
                  </tr>
                  <tr>
                    <td class="fw-semibold ps-3">Thời gian SQL Seek/Scan (Step 1 Ms)</td>
                    <td class="text-center fw-bold text-primary">{{ compareResult.v1.searchDebug?.step1Ms ?? 0 }} ms</td>
                    <td class="text-center fw-bold text-success">{{ compareResult.v2.searchDebug?.step1Ms ?? 0 }} ms</td>
                  </tr>
                  <tr>
                    <td class="fw-semibold ps-3">Thời gian Giải mã AES-256 (Step 2 Ms)</td>
                    <td class="text-center">{{ compareResult.v1.searchDebug?.step2Ms ?? 0 }} ms</td>
                    <td class="text-center">{{ compareResult.v2.searchDebug?.step2Ms ?? 0 }} ms</td>
                  </tr>
                  <tr>
                    <td class="fw-semibold ps-3">Số lượng Ứng viên (Candidate Count)</td>
                    <td class="text-center">{{ compareResult.v1.searchDebug?.candidateCount ?? 0 }}</td>
                    <td class="text-center">{{ compareResult.v2.searchDebug?.candidateCount ?? 0 }}</td>
                  </tr>
                  <tr>
                    <td class="fw-semibold ps-3">Số lượng Đụng độ / Nhiễu (Collision Count)</td>
                    <td class="text-center">{{ getCollisionCount(compareResult.v1) }}</td>
                    <td class="text-center">{{ getCollisionCount(compareResult.v2) }}</td>
                  </tr>
                  <tr>
                    <td class="fw-semibold ps-3">Số Kết quả Khớp Cuối cùng (Result Count)</td>
                    <td class="text-center fw-bold">{{ compareResult.v1.searchDebug?.resultCount ?? compareResult.v1.total }}</td>
                    <td class="text-center fw-bold">{{ compareResult.v2.searchDebug?.resultCount ?? compareResult.v2.total }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class BenchmarkComponent implements OnInit {
  @ViewChild('chart') chart?: ChartComponent;

  keyword: string = 'Mạc Thanh Trâm';
  field: string = 'Name';

  compareResult: CompareBenchmarkResult | null = null;
  chartOptions?: ChartOptions;

  constructor(
    private patientService: PatientService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    // Tự động kích hoạt so sánh ban đầu khi vào trang
    this.onCompare();
  }

  // Tính số lượng đụng độ cho từng pipeline
  getCollisionCount(side: { searchDebug?: any; total?: number; items?: any[] }): number {
    const debug = side?.searchDebug;
    if (!debug) return 0;
    if (debug.collisionCount !== undefined && debug.collisionCount !== null) {
      return debug.collisionCount;
    }
    const candidate = debug.candidateCount ?? 0;
    const result = debug.resultCount ?? side.total ?? side.items?.length ?? 0;
    return Math.max(0, candidate - result);
  }

  // Thực thi so sánh thuật toán V1 vs V2
  onCompare(): void {
    const trimmed = this.keyword.trim();
    if (!trimmed) {
      this.notificationService.showError('Vui lòng nhập từ khóa tìm kiếm (keyword).');
      return;
    }

    this.patientService.compareBenchmark(trimmed, this.field).subscribe({
      next: (res) => {
        this.compareResult = res;
        this.setupChart(res);
      }
    });
  }

  // Cấu hình dữ liệu biểu đồ ApexCharts Column Chart
  private setupChart(res: CompareBenchmarkResult): void {
    const v1 = res.v1.searchDebug;
    const v2 = res.v2.searchDebug;

    this.chartOptions = {
      series: [
        {
          name: 'V1 Baseline (SHA256)',
          data: [v1?.totalMs ?? 0, v1?.step1Ms ?? 0, v1?.step2Ms ?? 0]
        },
        {
          name: 'V2 Cải tiến (HMAC + BI-GRAM)',
          data: [v2?.totalMs ?? 0, v2?.step1Ms ?? 0, v2?.step2Ms ?? 0]
        }
      ],
      chart: {
        type: 'bar',
        height: 380,
        toolbar: { show: false }
      },
      colors: ['#0d6efd', '#198754'],
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '45%',
          borderRadius: 6
        }
      },
      dataLabels: {
        enabled: true,
        formatter: (val: number) => `${val} ms`
      },
      stroke: {
        show: true,
        width: 2,
        colors: ['transparent']
      },
      xaxis: {
        categories: ['Tổng thời gian (Total Ms)', 'Truy vấn SQL (Step 1 Ms)', 'Giải mã & Lọc (Step 2 Ms)']
      },
      tooltip: {
        y: {
          formatter: (val: number) => `${val} ms`
        }
      },
      legend: {
        position: 'top',
        horizontalAlign: 'center'
      }
    };
  }
}
