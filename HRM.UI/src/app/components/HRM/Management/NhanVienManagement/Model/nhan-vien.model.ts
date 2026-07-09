export interface NhanVienDTO {
  id_NV: number;
  maNV?: string;
  hoTen?: string;
  ngaySinh?: string;
  cmnd?: string;
  mobile?: string;
  email?: string;
  sotaikhoan?: string;
}

export interface NhanVienModel {
  maNV?: string;
  holot?: string;
  ten?: string;
  ngaySinh?: string;
  cmnd?: string;
  mobile?: string;
  email?: string;
  sotaikhoan?: string;
}

export interface SearchDebugInfo {
  searchKeyword?: string;
  searchType?: string;
  decryptedValue?: string;
  compareResult?: string;
  totalMs?: number;
  step1Ms?: number;
  step2Ms?: number;
  candidateCount?: number;
  scannedRecordCount?: number;
  collisionCount?: number;
  resultCount?: number;
}

export interface TableState {
  paginator: {
    pageIndex: number;
    pageSize: number;
    total: number;
  };
  filter: any;
  sorting: {
    column: string;
    direction: string;
  };
  searchTerm: string;
}
