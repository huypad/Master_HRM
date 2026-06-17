import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-base-tables-widget1',
  templateUrl: './base-tables-widget1.component.html',
  imports: [
    CommonModule, 
  ],
})
export class BaseTablesWidget1Component implements OnInit {
  TABS: string[] = [
    'Month',
    'Week',
    'Day'
  ];
  currentTab: any;
  @Input() cssClass!: string;
  @Input() progressWidth: any;

  constructor() { }

  ngOnInit(): void {
    if (!this.progressWidth) {
      this.progressWidth = 'min-w-200px';
    }
    this.currentTab = this.TABS[2];
  }

  setCurrentTab(tab: string) {
    this.currentTab = tab;
  }
}
