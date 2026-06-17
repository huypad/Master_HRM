import { DropdownMenu1Component } from '../../../dropdown-menus/dropdown-menu1/dropdown-menu1.component';
import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-lists-widget14',
  templateUrl: './lists-widget14.component.html',
  imports: [DropdownMenu1Component, CommonModule]
})
export class ListsWidget14Component implements OnInit {
  @Input() cssClass: any;
  @Input() rowNumber: any;
  constructor() { }

  ngOnInit(): void {
    if (!this.rowNumber) {
      this.rowNumber = 5;
    }
  }
}
