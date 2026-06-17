export const PageSizes = [3, 5, 10, 15, 50, 100];

export interface IPaginatorState {
  page: number;
  pageSize: number;
  total: number;
  recalculatePaginator(total: number): IPaginatorState;
}

export class PaginatorState {
  page: number = 1;
  pageSize: number = PageSizes[0];
  total: number = 0;
  pageSizes: number[] = PageSizes;
  totalpage: number = 0;
  // Optional: Thêm recalculatePaginator nếu cần
  recalculatePaginator(total: number): PaginatorState {
    this.total = total;
    this.totalpage = Math.ceil(total / this.pageSize);
    return this;
  }
}

export interface IPaginatorView {
  paginator: PaginatorState;
  ngOnInit(): void;
  paginate(paginator: PaginatorState): void;
}
