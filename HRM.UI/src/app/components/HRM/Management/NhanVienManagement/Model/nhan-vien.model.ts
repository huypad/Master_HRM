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