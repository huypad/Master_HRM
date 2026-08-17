// Model định nghĩa đối tượng Bệnh nhân (Patient) theo đúng định dạng JSON từ Backend
export interface Patient {
  patientID?: number;
  name: string;
  cccd: string;
  phone: string;
  bankAccount: string;
  age?: number | null;
  gender?: string | null;
  blood_Type?: string | null; // Chú ý: Có dấu gạch dưới theo Backend JSON
  email?: string | null;
}

// Model định nghĩa chỉ số Debug hiệu năng tra cứu
export interface SearchDebugInfo {
  searchKeyword: string;
  searchType: string;
  decryptedValue?: string | null;
  compareResult?: any;
  totalMs: number;       // Execution time (ms)
  step1Ms: number;       // SQL Scan time (ms)
  step2Ms: number;       // RAM Decrypt time (ms)
  candidateCount: number;
  scannedRecordCount: number;
  collisionCount: number; // collisionCount = candidateCount - resultCount
  resultCount: number;
}

// Interface định nghĩa Kết quả Tra cứu Lẻ
export interface SingleSearchResult {
  items: Patient[];
  total: number;
  searchDebug: SearchDebugInfo;
}

// Interface định nghĩa Kết quả So sánh Benchmark V1 vs V2
export interface CompareBenchmarkResult {
  keyword: string;
  field: string;
  v1: SingleSearchResult;
  v2: SingleSearchResult;
  comparison: {
    step1SpeedupRatio: number;
    totalSpeedupRatio: number;
    winner: string; // "V2" hoặc "V1"
  };
}
