import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../environments/environment';
import { UserDTO, LoginModel } from '../components/HRM/Management/AccountManagement/Model/account-management.model';
import { BehaviorSubject, Observable, of, Subscription } from 'rxjs';
import { map, catchError, finalize } from 'rxjs/operators';
import { throwError } from 'rxjs';

const AUTH_STORAGE_KEY = `${environment.appVersion || 'APP'}-auth`;

@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private currentUserSubject: BehaviorSubject<UserDTO | null>;
  public currentUser$: Observable<UserDTO | null>;

  private isLoadingSubject: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  public isLoading$: Observable<boolean> = this.isLoadingSubject.asObservable();

  private errorMessageSubject: BehaviorSubject<string> = new BehaviorSubject<string>('');
  public errorMessage$: Observable<string> = this.errorMessageSubject.asObservable();

  private subscriptions: Subscription[] = [];

  get currentUserValue(): UserDTO | null {
    return this.currentUserSubject.value;
  }

  set currentUserValue(user: UserDTO | null) {
    this.currentUserSubject.next(user);
  }

  get token(): string | null {
    return this.getAuthFromLocalStorage()?.token || null;
  }

  private apiUrl = environment.apiUrl + '/authorization';

  constructor(private http: HttpClient) {
    const stored = this.getAuthFromLocalStorage();
    this.currentUserSubject = new BehaviorSubject<UserDTO | null>(stored ? stored.user : null);
    this.currentUser$ = this.currentUserSubject.asObservable();
  }

  login(model: LoginModel): Observable<UserDTO | null> {
    this.isLoadingSubject.next(true);
    return this.http.post<{ user: UserDTO, token: string }>(`${this.apiUrl}/login`, model).pipe(
      map(res => {
        if (res && res.user && res.token) {
          this.setAuthToLocalStorage(res);
          return res.user;
        }
        return null;
      }),
      catchError(err => {
  return throwError(() => err); // đẩy lỗi về cho component xử lý
}),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  // Lưu thông tin user + token vào localStorage
  private setAuthToLocalStorage(auth: { user: UserDTO, token: string }) {
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
    this.currentUserSubject.next(auth.user);
  }

  // Lấy auth từ localStorage
  getAuthFromLocalStorage(): { user: UserDTO, token: string } | null {
    try {
      const raw = localStorage.getItem(AUTH_STORAGE_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  // Lấy user từ localStorage (nếu ko muốn dùng observable)
  getUser(): UserDTO | null {
    return this.getAuthFromLocalStorage()?.user || null;
  }

  // Lấy token từ localStorage
  getToken(): string | null {
    return this.getAuthFromLocalStorage()?.token || null;
  }

  // Xoá user và token khi logout
  logout() {
    localStorage.removeItem(AUTH_STORAGE_KEY);
    this.currentUserSubject.next(null);
  }

  // Thêm hàm kiểm tra đăng nhập
  isLoggedIn(): boolean {
    return !!this.currentUserSubject.value && !!this.getToken();
  }

  // Thêm hàm trả về HttpHeaders có Bearer token nếu cần
  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    });
  }

  updateLastlogin(): Observable<any> {
  const url = `${environment.apiUrl}/auth/UpdateLastLogin`;
  return this.http.post<any>(url, {}, { headers: this.getAuthHeaders() });
}

  // Cleanup subscription nếu dùng thêm Observable khác
  ngOnDestroy() {
    this.subscriptions.forEach(sb => sb.unsubscribe());
  }
}
