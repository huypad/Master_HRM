import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { MenuItem, MenuConfig } from '../models/menu-item.interface';
import { environment } from '../../../environments/environment';
@Injectable({
  providedIn: 'root',
})
export class DynamicAsideMenuService {
  private menuConfigSubject = new BehaviorSubject<MenuConfig>({ items: [] });
  public menuConfig$ = this.menuConfigSubject.asObservable();

  private baseUrl = `${environment.apiUrl}/menu`;

  constructor(private http: HttpClient) { }

  public loadMenu(): void {
    this.http.get<any>(`${this.baseUrl}/LayMenuChucNang`).subscribe({
      next: (response) => {
        if (response.statusCode === 1 && Array.isArray(response.data)) {
          const items: MenuItem[] = response.data.map((menu: any) => ({
            title: menu.summary || menu.title,
            page: this.mapLink(menu.aLink),
            icon: menu.icon,
            submenu: Array.isArray(menu.child) && menu.child.length > 0
              ? menu.child.map((sub: any) => ({
                title: sub.summary || sub.title,
                page: this.mapLink(sub.aLink),
                icon: sub.icon,
              }))
              : undefined,
          }));

          this.menuConfigSubject.next({ items });
        } else {
          this.menuConfigSubject.next({ items: [] });
        }
      },
      error: (error) => {
        this.menuConfigSubject.next({ items: [] });
      },
    });
  }

  private mapLink(rawLink: string | undefined): string {
    if (!rawLink) return '/';
    return '/' + rawLink
      .replace(/^Management\//i, '')
      .replace(/Management$/i, '')
      .trim()
      .toLowerCase();
  }

  public Test500(): void {
    this.http.get<any>(`${this.baseUrl}/apiTest500`).subscribe();
  }
}
