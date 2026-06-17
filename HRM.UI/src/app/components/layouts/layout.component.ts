import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  AfterViewInit,
} from '@angular/core';
import { LayoutService, LayoutInitService } from '../../_metronic/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SubheaderWrapperComponent } from '../../_metronic/partials/layout/subheader/subheader-wrapper/subheader-wrapper.component';
import { UserOffcanvasComponent } from '../../_metronic/partials/layout/extras/offcanvas/user-offcanvas/user-offcanvas.component';
import { ScrollTopComponent } from '../../_metronic/partials/layout/extras/scroll-top/scroll-top.component';
import { ToolbarComponent } from '../../_metronic/partials/layout/extras/toolbar/toolbar.component';
import { ScriptsInitComponent } from '../layouts/init/scipts-init/scripts-init.component';
import { HeaderComponent } from './components/header/header.component';
import { DefaultLayoutConfig } from '../../_metronic/configs/default-layout.config';
import { AsideDynamicComponent } from './components/aside-dynamic/aside-dynamic.component';
import { MenuServices } from '../../_metronic/core/services/menu.service';

declare var KTLayoutContent: any;

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    UserOffcanvasComponent,
    ToolbarComponent,
    ScriptsInitComponent,
    ScrollTopComponent, HeaderComponent, AsideDynamicComponent,
  ],
  providers: [MenuServices],
})
export class LayoutComponent implements OnInit, AfterViewInit {
  // Public variables with proper types and defaults
  selfLayout: string = 'default';
  asideSelfDisplay: boolean = true;
  asideMenuStatic: boolean = true;
  contentClasses: string = '';
  contentContainerClasses: string = '';
  subheaderDisplay: boolean = true;
  contentExtended: boolean = false; // Đặt mặc định, sẽ được cập nhật từ LayoutService
  asideCSSClasses: string = '';
  asideHTMLAttributes: { [key: string]: any } = {};
  headerMobileClasses: string = '';
  headerMobileAttributes: { [key: string]: any } = {};
  footerDisplay: boolean = false; // Đặt mặc định, sẽ được cập nhật từ LayoutService
  footerCSSClasses: string = '';
  headerCSSClasses: string = '';
  headerHTMLAttributes: { [key: string]: any } = {};

  // Offcanvas settings
  extrasSearchOffcanvasDisplay: boolean = false;
  extrasNotificationsOffcanvasDisplay: boolean = false;
  extrasQuickActionsOffcanvasDisplay: boolean = false;
  extrasCartOffcanvasDisplay: boolean = false;
  extrasUserOffcanvasDisplay: boolean = false;
  extrasQuickPanelDisplay: boolean = false;
  extrasScrollTopDisplay: boolean = false;

  @ViewChild('ktAside', { static: true }) ktAside!: ElementRef;
  @ViewChild('ktHeaderMobile', { static: true }) ktHeaderMobile!: ElementRef;
  @ViewChild('ktHeader', { static: true }) ktHeader!: ElementRef;

  constructor(
    private initService: LayoutInitService,
    private layout: LayoutService
  ) {
    this.initService.init();
  }

  ngOnInit(): void {
    this.selfLayout = this.layout.getProp('self.layout') || 'default';
    console.log('selfLayout:', this.selfLayout); // Kiểm tra giá trị
    // Build view by layout config settings
    this.selfLayout = this.layout.getProp('self.layout') || 'default';
    this.asideSelfDisplay = this.layout.getProp('aside.self.display') || true;
    this.asideMenuStatic = this.layout.getProp('aside.menu.static') || true;
    this.subheaderDisplay = this.layout.getProp('subheader.display') || true;
    this.contentClasses = this.layout.getStringCSSClasses('content') || '';
    this.contentContainerClasses = this.layout.getStringCSSClasses('content_container') || '';
    this.contentExtended = this.layout.getProp('content.extended') || false;
    this.asideHTMLAttributes = this.layout.getHTMLAttributes('aside') || {};
    this.asideCSSClasses = this.layout.getStringCSSClasses('aside') || '';
    this.headerMobileClasses = this.layout.getStringCSSClasses('header_mobile') || '';
    this.headerMobileAttributes = this.layout.getHTMLAttributes('header_mobile') || {};
    this.footerDisplay = this.layout.getProp('footer.display') || false;
    this.footerCSSClasses = this.layout.getStringCSSClasses('footer') || '';
    this.headerCSSClasses = this.layout.getStringCSSClasses('header') || '';
    this.headerHTMLAttributes = this.layout.getHTMLAttributes('header') || {};

    // Offcanvas settings
    if (this.layout.getProp('extras.search.display')) {
      this.extrasSearchOffcanvasDisplay = this.layout.getProp('extras.search.layout') === 'offcanvas';
    }
    if (this.layout.getProp('extras.notifications.display')) {
      this.extrasNotificationsOffcanvasDisplay = this.layout.getProp('extras.notifications.layout') === 'offcanvas';
    }
    if (this.layout.getProp('extras.quickActions.display')) {
      this.extrasQuickActionsOffcanvasDisplay = this.layout.getProp('extras.quickActions.layout') === 'offcanvas';
    }
    if (this.layout.getProp('extras.cart.display')) {
      this.extrasCartOffcanvasDisplay = this.layout.getProp('extras.cart.layout') === 'offcanvas';
    }
    if (this.layout.getProp('extras.user.display')) {
      this.extrasUserOffcanvasDisplay = this.layout.getProp('extras.user.layout') === 'offcanvas';
    }
    this.extrasQuickPanelDisplay = this.layout.getProp('extras.quickPanel.display') || false;
    this.extrasScrollTopDisplay = this.layout.getProp('extras.scrolltop.display') || false;

  }

  ngAfterViewInit(): void {
    if (this.ktAside) {
      for (const key in this.asideHTMLAttributes) {
        if (this.asideHTMLAttributes.hasOwnProperty(key)) {
          this.ktAside.nativeElement.setAttribute(key, this.asideHTMLAttributes[key]);
        }
      }
    }

    if (this.ktHeaderMobile) {
      for (const key in this.headerMobileAttributes) {
        if (this.headerMobileAttributes.hasOwnProperty(key)) {
          this.ktHeaderMobile.nativeElement.setAttribute(key, this.headerMobileAttributes[key]);
        }
      }
    }

    if (this.ktHeader) {
      for (const key in this.headerHTMLAttributes) {
        if (this.headerHTMLAttributes.hasOwnProperty(key)) {
          this.ktHeader.nativeElement.setAttribute(key, this.headerHTMLAttributes[key]);
        }
      }
    }
    KTLayoutContent.init('kt_content');
  }
}