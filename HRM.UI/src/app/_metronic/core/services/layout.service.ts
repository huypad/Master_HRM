import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import * as objectPath from 'object-path';
import { DefaultLayoutConfig } from '../../configs/default-layout.config';

// Interface cho cấu hình layout
interface LayoutConfig {
  [key: string]: any;
}

// Interface cho classes
interface Classes {
  [key: string]: string[];
  header: string[];
  header_container: string[];
  header_mobile: string[];
  header_menu: string[];
  aside_menu: string[];
  subheader: string[];
  subheader_container: string[];
  content: string[];
  content_container: string[];
  footer_container: string[];
}

// Interface cho attrs
interface Attrs {
  [key: string]: { [key: string]: any };
  aside_menu: { [key: string]: any };
}

const LAYOUT_CONFIG_LOCAL_STORAGE_KEY = `${environment.appVersion}-layoutConfig`;

@Injectable({
  providedIn: 'root',
})
export class LayoutService {
  private layoutConfigSubject: BehaviorSubject<LayoutConfig> = new BehaviorSubject<LayoutConfig>(DefaultLayoutConfig);

  private classes: Classes = {
    header: [],
    header_container: [],
    header_mobile: [],
    header_menu: [],
    aside_menu: [],
    subheader: [],
    subheader_container: [],
    content: [],
    content_container: [],
    footer_container: [],
  };

  private attrs: Attrs = {
    aside_menu: {},
  };

  constructor() {
    this.initConfig(); // Khởi tạo ngay trong constructor
    console.log('LayoutService initialized with config:', DefaultLayoutConfig); // Debug
  }

  initConfig(): void {
    const configFromLocalStorage = localStorage.getItem(LAYOUT_CONFIG_LOCAL_STORAGE_KEY);
    if (configFromLocalStorage) {
      try {
        this.layoutConfigSubject.next(JSON.parse(configFromLocalStorage));
        return;
      } catch (error) {
        this.removeConfig();
        console.error('config parse from local storage', error);
      }
    }
    this.layoutConfigSubject.next(DefaultLayoutConfig);
  }

  private removeConfig(): void {
    localStorage.removeItem(LAYOUT_CONFIG_LOCAL_STORAGE_KEY);
  }

  refreshConfigToDefault(): void {
    this.setConfigWithPageRefresh(DefaultLayoutConfig);
  }

  getConfig(): LayoutConfig {
    return this.layoutConfigSubject.value;
  }

  setConfig(config: LayoutConfig): void {
    if (!config) {
      this.removeConfig();
      this.layoutConfigSubject.next(DefaultLayoutConfig);
    } else {
      localStorage.setItem(LAYOUT_CONFIG_LOCAL_STORAGE_KEY, JSON.stringify(config));
      this.layoutConfigSubject.next(config);
    }
  }

  setConfigWithoutLocalStorageChanges(config: LayoutConfig): void {
    this.layoutConfigSubject.next(config);
  }

  setConfigWithPageRefresh(config: LayoutConfig): void {
    this.setConfig(config);
    document.location.reload();
  }

  getProp(path: string): any {
    return objectPath.get(this.layoutConfigSubject.value, path);
  }

  setCSSClass(path: keyof Classes, classesInStr: string): void {
    const cssClasses = this.classes[path] || (this.classes[path] = []);
    classesInStr.split(' ').forEach((cssClass: string) => cssClasses.push(cssClass));
  }

  getCSSClasses(path: keyof Classes): string[] {
    return this.classes[path] || [];
  }

  getStringCSSClasses(path: keyof Classes): string {
    return this.getCSSClasses(path).join(' ');
  }

  getHTMLAttributes(path: keyof Attrs): { [key: string]: any } {
    return this.attrs[path] || {};
  }

  setHTMLAttribute(path: keyof Attrs, attrKey: string, attrValue: any): void {
    const attributesObj = this.attrs[path] || (this.attrs[path] = {});
    attributesObj[attrKey] = attrValue;
  }
}