import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PerfectScrollbarModule } from 'ngx-perfect-scrollbar';
import { PERFECT_SCROLLBAR_CONFIG } from 'ngx-perfect-scrollbar';
import { PerfectScrollbarConfigInterface } from 'ngx-perfect-scrollbar';
import { SearchDropdownInnerComponent } from './dropdown-inner/search-dropdown-inner/search-dropdown-inner.component';
import { NotificationsDropdownInnerComponent } from './dropdown-inner/notifications-dropdown-inner/notifications-dropdown-inner.component';
import { QuickActionsDropdownInnerComponent } from './dropdown-inner/quick-actions-dropdown-inner/quick-actions-dropdown-inner.component';
import { CartDropdownInnerComponent } from './dropdown-inner/cart-dropdown-inner/cart-dropdown-inner.component';
import { UserDropdownInnerComponent } from './dropdown-inner/user-dropdown-inner/user-dropdown-inner.component';
import { SearchOffcanvasComponent } from './offcanvas/search-offcanvas/search-offcanvas.component';
import { SearchResultComponent } from './dropdown-inner/search-dropdown-inner/search-result/search-result.component';
import { NotificationsOffcanvasComponent } from './offcanvas/notifications-offcanvas/notifications-offcanvas.component';
import { QuickActionsOffcanvasComponent } from './offcanvas/quick-actions-offcanvas/quick-actions-offcanvas.component';
import { CartOffcanvasComponent } from './offcanvas/cart-offcanvas/cart-offcanvas.component';
import { QuickPanelOffcanvasComponent } from './offcanvas/quick-panel-offcanvas/quick-panel-offcanvas.component';
import { UserOffcanvasComponent } from './offcanvas/user-offcanvas/user-offcanvas.component';
import { ScrollTopComponent } from './scroll-top/scroll-top.component';
import { ToolbarComponent } from './toolbar/toolbar.component';
import { ChangePasswordEditDialogComponent } from './offcanvas/change-password-dialog/change-password-dialog.component';
import { Routes } from '@angular/router';

export const extrasRoutes: Routes = [
  {
    path: 'search-dropdown',
    component: SearchDropdownInnerComponent,
  },
  {
    path: 'notifications-dropdown',
    component: NotificationsDropdownInnerComponent,
  },
  {
    path: 'quick-actions-dropdown',
    component: QuickActionsDropdownInnerComponent,
  },
  {
    path: 'cart-dropdown',
    component: CartDropdownInnerComponent,
  },
  {
    path: 'user-dropdown',
    component: UserDropdownInnerComponent,
  },
  {
    path: 'search-offcanvas',
    component: SearchOffcanvasComponent,
  },
  {
    path: 'notifications-offcanvas',
    component: NotificationsOffcanvasComponent,
  },
  {
    path: 'quick-actions-offcanvas',
    component: QuickActionsOffcanvasComponent,
  },
  {
    path: 'cart-offcanvas',
    component: CartOffcanvasComponent,
  },
  {
    path: 'quick-panel-offcanvas',
    component: QuickPanelOffcanvasComponent,
  },
  {
    path: 'user-offcanvas',
    component: UserOffcanvasComponent,
  },
  {
    path: 'scroll-top',
    component: ScrollTopComponent,
  },
  {
    path: 'toolbar',
    component: ToolbarComponent,
  },
  {
    path: 'change-password',
    component: ChangePasswordEditDialogComponent,
  },
];
