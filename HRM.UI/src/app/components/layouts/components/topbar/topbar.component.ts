import { Component, OnInit, AfterViewInit } from '@angular/core';
import { Observable } from 'rxjs';
import { LayoutService } from '../../../../_metronic/core';
import { AuthService } from '../../../../services/auth.service';
import { UserDTO, UserModel } from '../../../../components/HRM/Management/AccountManagement/Model/account-management.model';
declare var KTLayoutQuickSearch: any;
declare var KTLayoutQuickNotifications: any;
declare var KTLayoutQuickActions: any;
declare var KTLayoutQuickCartPanel: any;
declare var KTLayoutQuickPanel: any;
declare var KTLayoutQuickUser: any;
declare var KTLayoutHeaderTopbar: any;
declare var KTUtil : any;

import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
  imports: [
    ReactiveFormsModule,
    FormsModule,
    CommonModule, ReactiveFormsModule
  ],
})
export class TopbarComponent implements OnInit, AfterViewInit {
  user: UserDTO | null;
  // tobbar extras
  extraSearchDisplay!: boolean;
  extrasSearchLayout!: 'offcanvas' | 'dropdown';
  extrasNotificationsDisplay!: boolean;
  extrasNotificationsLayout!: 'offcanvas' | 'dropdown';
  extrasQuickActionsDisplay!: boolean;
  extrasQuickActionsLayout!: 'offcanvas' | 'dropdown';
  extrasCartDisplay!: boolean;
  extrasCartLayout!: 'offcanvas' | 'dropdown';
  extrasQuickPanelDisplay!: boolean;
  extrasLanguagesDisplay!: boolean;
  extrasUserDisplay!: boolean;
  extrasUserLayout!: 'offcanvas' | 'dropdown';

  constructor(private layout: LayoutService, private auth: AuthService) {
    this.user = this.auth.getUser();
  }

  ngOnInit(): void {
    // topbar extras
    this.extraSearchDisplay = this.layout.getProp('extras.search.display');
    this.extrasSearchLayout = this.layout.getProp('extras.search.layout');
    this.extrasNotificationsDisplay = this.layout.getProp('extras.notifications.display');
    this.extrasNotificationsLayout = this.layout.getProp('extras.notifications.layout');
    this.extrasQuickActionsDisplay = this.layout.getProp('extras.quickActions.display');
    this.extrasQuickActionsLayout = this.layout.getProp('extras.quickActions.layout');
    this.extrasCartDisplay = this.layout.getProp('extras.cart.display');
    this.extrasCartLayout = this.layout.getProp('extras.cart.layout');
    this.extrasLanguagesDisplay = this.layout.getProp('extras.languages.display');
    this.extrasUserDisplay = this.layout.getProp('extras.user.display');
    this.extrasUserLayout = this.layout.getProp('extras.user.layout');
    this.extrasQuickPanelDisplay = this.layout.getProp('extras.quickPanel.display');
  }

  ngAfterViewInit(): void {
    KTUtil.ready(() => {
      // Called after ngAfterContentInit when the component's view has been initialized. Applies to components only.
      // Add 'implements AfterViewInit' to the class.
      if (this.extraSearchDisplay && this.extrasSearchLayout === 'offcanvas') {
        KTLayoutQuickSearch.init('kt_quick_search');
      }

      if (this.extrasNotificationsDisplay && this.extrasNotificationsLayout === 'offcanvas') {
        // Init Quick Notifications Offcanvas Panel
        KTLayoutQuickNotifications.init('kt_quick_notifications');
      }

      if (this.extrasQuickActionsDisplay && this.extrasQuickActionsLayout === 'offcanvas') {
        // Init Quick Actions Offcanvas Panel
        KTLayoutQuickActions.init('kt_quick_actions');
      }

      if (this.extrasCartDisplay && this.extrasCartLayout === 'offcanvas') {
        // Init Quick Cart Panel
        KTLayoutQuickCartPanel.init('kt_quick_cart');
      }

      if (this.extrasQuickPanelDisplay) {
        // Init Quick Offcanvas Panel
        KTLayoutQuickPanel.init('kt_quick_panel');
      }

      if (this.extrasUserDisplay && this.extrasUserLayout === 'offcanvas') {
        // Init Quick User Panel
        KTLayoutQuickUser.init('kt_quick_user');
      }

      // Init Header Topbar For Mobile Mode
      KTLayoutHeaderTopbar.init('kt_header_mobile_topbar_toggle');
    });
  }
}
