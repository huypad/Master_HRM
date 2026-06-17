import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-base-tables-widget6',
  templateUrl: './base-tables-widget6.component.html',
  imports: [
    CommonModule, 
  ],
})
export class BaseTablesWidget6Component implements OnInit {
  TABS: string[] = [
    'Month',
    'Week',
    'Day'
  ];
  currentTab: any;
  @Input() cssClass!: string;

  constructor() { }

  ngOnInit(): void {
    this.currentTab = this.TABS[2];
  }

  setCurrentTab(tab: string) {
    this.currentTab = tab;
  }
}
