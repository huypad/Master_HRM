import { Component, Input } from '@angular/core';
import { DropdownMenu1Component } from '../../../dropdown-menus/dropdown-menu1/dropdown-menu1.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-lists-widget8',
  templateUrl: './lists-widget8.component.html',
  imports: [DropdownMenu1Component, CommonModule]
})
export class ListsWidget8Component {
  @Input() cssClass: any;
  constructor() { }
}
