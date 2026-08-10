import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  Patient,
  SingleSearchResult,
  CompareBenchmarkResult
} from '../models/patient.model';

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // 1. Lấy danh sách bệnh nhân phân trang trên CSDL HealthcareDB (Màn 1 & Màn 2)
  getPaged(page: number = 1, pageSize: number = 10): Observable<{ items: Patient[]; total: number }> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<{ items: Patient[]; total: number }>(`${this.baseUrl}/patient`, { params });
  }

  // 2. Lấy chi tiết hồ sơ bệnh nhân theo ID
  getById(id: number): Observable<Patient> {
    return this.http.get<Patient>(`${this.baseUrl}/patient/${id}`);
  }

  // 3. Thêm mới bệnh nhân (POST /api/patient)
  create(patient: Omit<Patient, 'patientID'>): Observable<Patient> {
    return this.http.post<Patient>(`${this.baseUrl}/patient`, patient);
  }

  // 4. Cập nhật thông tin bệnh nhân (PUT /api/patient/{id})
  update(id: number, patient: Omit<Patient, 'patientID'>): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/patient/${id}`, patient);
  }

  // 5. Xóa bệnh nhân theo ID (DELETE /api/patient/{id})
  delete(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/patient/${id}`);
  }

  // 6. Tra cứu Benchmark Lẻ (GET /api/benchmark/search)
  searchBenchmark(
    keyword: string,
    field: string = 'Name',
    pipeline: string = 'V2'
  ): Observable<SingleSearchResult> {
    const params = new HttpParams()
      .set('keyword', keyword)
      .set('field', field)
      .set('pipeline', pipeline);

    return this.http.get<SingleSearchResult>(`${this.baseUrl}/benchmark/search`, { params });
  }

  // 7. So sánh hiệu năng thuật toán V1 Baseline vs V2 BI-GRAM (GET /api/benchmark/compare)
  compareBenchmark(
    keyword: string,
    field: string = 'Name'
  ): Observable<CompareBenchmarkResult> {
    const params = new HttpParams()
      .set('keyword', keyword)
      .set('field', field);

    return this.http.get<CompareBenchmarkResult>(`${this.baseUrl}/benchmark/compare`, { params });
  }
}
