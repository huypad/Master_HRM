import { Component, OnInit } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-notifications-offcanvas',
  templateUrl: './notifications-offcanvas.component.html',
  styleUrls: ['./notifications-offcanvas.component.scss'],
  imports: [
    CommonModule, 
  ],
})
export class NotificationsOffcanvasComponent implements OnInit {
  extrasNotificationsOffcanvasDirectionCSSClass: string = '';

  constructor(private layout: LayoutService) {}
  ngOnInit(): void {
    this.extrasNotificationsOffcanvasDirectionCSSClass = `offcanvas-${this.layout.getProp(
      'extras.notifications.offcanvas.direction'
    )}`;
  }
}
