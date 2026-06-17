import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, finalize, switchMap, tap } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
  NhanVienDTO,
  NhanVienModel,
  TableState,
} from '../Model/nhan-vien.model';

type AccessMode = 'public' | 'private';

interface NhanVienPagedResponse {
  items: NhanVienDTO[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class NhanVienService {
  private apiUrl = environment.apiUrl + '/nhanvien';

  private accessMode: AccessMode = 'private';

  private _items$ = new BehaviorSubject<NhanVienDTO[]>([]);
  private _isLoading$ = new BehaviorSubject<boolean>(false);

  private _tableState$ = new BehaviorSubject<TableState>({
    paginator: { pageIndex: 0, pageSize: 10, total: 0 },
    filter: {},
    sorting: { column: '', direction: '' },
    searchTerm: '',
  });

  private loadTrigger$ = new Subject<{
    keyword: string;
    page: number;
    pageSize: number;
    sortColumn?: string;
    sortDirection?: string;
  }>();

  items$ = this._items$.asObservable();
  isLoading$ = this._isLoading$.asObservable();
  tableState$ = this._tableState$.asObservable();

  constructor(private http: HttpClient) {
    this.loadTrigger$
      .pipe(
        tap(() => this._isLoading$.next(true)),
        switchMap(({ keyword, page, pageSize, sortColumn, sortDirection }) => {
          const trimmedKeyword = (keyword || '').trim();
          const url = trimmedKeyword ? `${this.apiUrl}/search` : this.apiUrl;

          const params: any = {
            page,
            pageSize,
            accessMode: this.accessMode,
          };

          if (sortColumn) {
            params.sortColumn = sortColumn;
          }

          if (sortDirection) {
            params.sortDirection = sortDirection;
          }

          if (trimmedKeyword) {
            params.keyword = trimmedKeyword;
          }

          return this.http
            .get<NhanVienPagedResponse>(url, { params })
            .pipe(
              tap((res) => console.log('NhanVien API response:', res)),
              catchError((err) => {
                console.error('NhanVien API error:', err);
                return of({ items: [], total: 0 });
              }),
              finalize(() => this._isLoading$.next(false))
            );
        })
      )
      .subscribe((res) => {
        const current = this._tableState$.value;

        this._items$.next(res.items ?? []);

        this._tableState$.next({
          ...current,
          paginator: {
            ...current.paginator,
            total: res.total ?? 0,
          },
        });
      });
  }

  getAccessMode(): AccessMode {
    return this.accessMode;
  }

  setAccessMode(mode: AccessMode): void {
    this.accessMode = mode;

    const state = this._tableState$.value;

    this.load(
      state.searchTerm,
      1,
      state.paginator.pageSize,
      state.sorting.column,
      state.sorting.direction
    );
  }

  load(
    keyword = '',
    page = 1,
    pageSize = 10,
    sortColumn = '',
    sortDirection = ''
  ): void {
    const trimmedKeyword = (keyword || '').trim();

    this._tableState$.next({
      ...this._tableState$.value,
      searchTerm: trimmedKeyword,
      sorting: {
        column: sortColumn || '',
        direction: sortDirection || '',
      },
      paginator: {
        ...this._tableState$.value.paginator,
        pageIndex: page - 1,
        pageSize,
      },
    });

    this.loadTrigger$.next({
      keyword: trimmedKeyword,
      page,
      pageSize,
      sortColumn,
      sortDirection,
    });
  }

  reload(): void {
    const state = this._tableState$.value;

    this.load(
      state.searchTerm,
      state.paginator.pageIndex + 1,
      state.paginator.pageSize,
      state.sorting.column,
      state.sorting.direction
    );
  }

  getById(id: number): Observable<NhanVienDTO> {
    return this.http.get<NhanVienDTO>(`${this.apiUrl}/${id}`, {
      params: {
        accessMode: this.accessMode,
      },
    });
  }

  create(model: NhanVienModel): Observable<any> {
    return this.http.post(this.apiUrl, model);
  }

  update(id: number, model: NhanVienModel): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, model);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  rebuildSecureIndexBulk(batchSize = 100): Observable<any> {
    return this.http.post(`${this.apiUrl}/rebuild-secure-index-bulk`, null, {
      params: {
        batchSize,
      },
    });
  }
}