import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-notifications-dropdown-inner',
  templateUrl: './notifications-dropdown-inner.component.html',
  styleUrls: ['./notifications-dropdown-inner.component.scss'],
  imports: [
    CommonModule, 
  ],
})
export class NotificationsDropdownInnerComponent implements OnInit {
  extrasNotificationsDropdownStyle: 'light' | 'dark' = 'dark';
  activeTabId:
    | 'topbar_notifications_notifications'
    | 'topbar_notifications_events'
    | 'topbar_notifications_logs' = 'topbar_notifications_notifications';
  constructor(private layout: LayoutService) {}

  ngOnInit(): void {
    this.extrasNotificationsDropdownStyle = this.layout.getProp(
      'extras.notifications.dropdown.style'
    );
  }

  setActiveTabId(tabId: any) {
    this.activeTabId = tabId;
  }

  getActiveCSSClasses(tabId: any) {
    if (tabId !== this.activeTabId) {
      return '';
    }
    return 'active show';
  }
}
