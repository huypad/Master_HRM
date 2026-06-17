// menu-config.service.ts
import { Injectable } from '@angular/core';
import { MenuServices } from './menu.service';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { MenuConfig } from '../models/menu-item.interface';

@Injectable({
  providedIn: 'root'
})
export class MenuConfigService {
  constructor(private menuService: MenuServices) {}

  getMenus(): Observable<MenuConfig> {
    return this.menuService.layMenuChucNang().pipe(
      map((res) => {
        return {
          items: res.items || []
        } as MenuConfig;
      })
    );
  }
}
